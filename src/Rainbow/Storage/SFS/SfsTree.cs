using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rainbow.Formatting;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.IO;
using Sitecore.StringExtensions;

namespace Rainbow.Storage.SFS
{
	/*
	- Deep FS b-tree store. Implements a filesystem based b-tree of n degree, and handles the pathing thereof. Does NOT handle file formatting. DOES handle streams.
	- Moving nodes or renaming nodes will be handled by deleting and readding, at least for now. Also removes need for a predicate to test moved items for inclusion.
	- The b-tree will be traversable using tree walking, but not by ID, template, etc
	- The b-tree will not know or care about source pathing or databases (e.g. /sitecore/content/etc)
	- The b-tree will accomodate same-named items. Path based queries will return all items of a name in a path.
		Naming algorithm, prototype:
		- Default name is [itemname].[ext]
		- On save/add, use Directory.GetFiles(dir, "[name]_*.[ext]") to find all possible file names
		- For GetByPath(): read and return all
		- For Save(): read all, try to match by ID - if no matches, add new, otherwise update
			- Add new algorithm:
				Set name to [name]-[id].[ext]
				Note: reserialize may change the name of the item, if the original serializes after this one. Them's the breaks.
		- For Remove(): read all, try to match by ID - if no matches do nothing
	- The resultant node item should lazy load the actual file, so that we can tree walk the hierarchy without reading every inode's data
	- Supported queries: 
		Node GetRootNode(), 
		Node[] GetChildrenOfNode(Node n), - disambiguates child using parent node's ID, if multiple children at path exist
		void Remove(Node n), - note recursive automatically
		void Save(Node child) - note save will add or update existing node with same ID
		Node[] GetNodeByPath(string path) - returns all items of a given name at a given path. Caller can figure out which to use.

	- Dependencies: File Formatter

	- foo
		- bar
			- baz
			(ghost)
	- Subtrees
		- (id of baz)
			- bonk
				- schlonk
					- (ghost)
		- (id of schlonk)
			- honk
				- wonk
	*/
	/// <summary>
	/// Represents a tree of items in a Serialization File System (SFS) organization
	/// SFS is similar to Sitecore standard, but accounts for things that Sitecore does not do very well,
	/// like items with the same name in the same path and the ability to use local vs global paths
	/// </summary>
	public class SfsTree
	{
		private readonly string _globalRootItemPath;
		private readonly string _physicalRootPath;
		private readonly ISerializationFormatter _formatter;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">A name for this tree, for your reference</param>
		/// <param name="globalRootItemPath">The 'global' path where this tree is rooted. For example, if this was '/sitecore/content', the root item in this tree would be 'content'</param>
		/// <param name="databaseName">Name of the database the items in this tree are from. This is for your reference and help when resolving this tree as a destination, and is not directly used.</param>
		/// <param name="physicalRootPath">The physical root path to write items in this tree to. Will be created if it does not exist.</param>
		/// <param name="formatter">The formatter to use when reading or writing items to disk</param>
		public SfsTree(string name, string globalRootItemPath, string databaseName, string physicalRootPath, ISerializationFormatter formatter)
		{
			_globalRootItemPath = globalRootItemPath;
			_physicalRootPath = physicalRootPath;
			_formatter = formatter;
			Name = name;
			DatabaseName = databaseName;
		}

		public string DatabaseName { get; private set; }
		public string Name { get; private set; }

		public bool ContainsPath(string globalPath)
		{
			// test that the path is under the global root
			return globalPath.StartsWith(_globalRootItemPath, StringComparison.OrdinalIgnoreCase);
		}

		public IItemData GetRootItem()
		{
			var rootItem = Directory.GetFiles(_physicalRootPath, "*" + _formatter.FileExtension);

			if (rootItem.Length == 0) return null;

			if (rootItem.Length > 1) throw new InvalidOperationException("Found multiple root items in " + _physicalRootPath + "! This is not valid: a tree may only have one root inode.");

			return ReadItem(rootItem[0]);
		}

		public IEnumerable<IItemData> GetItemsByPath(string globalPath)
		{
			var localPath = ConvertGlobalPathToTreePath(globalPath);

			return GetPhysicalFilePathsForVirtualPath(localPath).Select(ReadItem);
		}

		public IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			return GetChildPaths(parentItem).Select(ReadItem);
		}

		/*
		REMOVING ITEMS, GIVEN A VIRTUAL PATH AND ID

			1. Determine the physical path of the item to get children for (use 'finding file paths, given a virtual path'), compare matches against ID
			2. Use 'finding child paths, given a physical path' recursively to find all descendant items including through loopbacks
			3. Starting at the deepest paths, begin deleting item files and - if present - children subfolders, until all are gone
		*/
		public bool Remove(IItemData item)
		{
			var localPath = ConvertGlobalPathToTreePath(item.Path);

			var itemToRemove = GetItemForVirtualPath(localPath, item.Id);

			if (itemToRemove == null) return false;

			var descendants = GetDescendants(item);

			foreach (var descendant in descendants.OrderByDescending(desc => desc.Path))
			{
				lock (FileUtil.GetFileLock(descendant.SerializedItemId))
				{
					File.Delete(descendant.SerializedItemId);
				}
			}

			return true;
		}

		/*
		Saving a changed item involves:
		- Get the parent item from the tree, if it exists (error if not exists)
		- Save the child item into the tree (replace existing if same ID, or add new if no matching name OR no matching ID)
		*/
		public void Save(IItemData item)
		{
			var storagePath = GetTargetPhysicalPath(item);

			WriteItem(item, storagePath);
		}

		protected virtual IItemData ReadItem(string path)
		{
			lock (FileUtil.GetFileLock(path))
			{
				using (var reader = File.OpenRead(path))
				{
					return _formatter.ReadSerializedItem(reader, path);
				}
			}
		}

		protected virtual void WriteItem(IItemData item, string path)
		{
			lock (FileUtil.GetFileLock(path))
			{
				using (var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					_formatter.WriteSerializedItem(item, writer);
				}
			}
		}

		protected virtual string ConvertGlobalPathToTreePath(string globalPath)
		{
			if (!globalPath.StartsWith(_globalRootItemPath)) throw new InvalidOperationException("The global path {0} was not rooted under the local item root path {1}. This means you tried to put an item where it didn't belong.".FormatWith(globalPath, _globalRootItemPath));

			return globalPath.Substring(_globalRootItemPath.Length);
		}

		/*
		CALCULATING AN ITEM'S PATH TO SAVE TO, GIVEN A WHOLE ITEM WITH PARENT ID, VIRTUAL PATH
	
				1. Start by using "finding file paths, given a virtual path" on the parent path of the item
				2. If no matches exist, throw - parent must be serialized
				3. If multiple matches exist, narrow them by the parent ID of the item - if no matches, throw
				4. If one match, parent path is found
				4.5. Determine if this item has any name-dupes in the source store (use 'finding child paths, given a physical path', and filter on the same name prefix). 
					- If no (and no can include 'yes, but this item exists and has an unescaped name already so we want to reuse that'), push its name onto the path string. 
					- If yes, escape it and push that onto the path string
				5. Strip characters not allowed by the filesystem from the base path
				6. The path string is now the 'base' path, which may be too long to use
				7. Determine if the base-string is over-length (which would be 240-$(Serialization.SerializationFolderPathMaxLength))
					- If the base string is short enough, return it
				8. Break up the path into an array. Loop over it, counting length up until you find the node that gets too long
				9. Start up a new path parts list, and as you keep looping over the parts of the base string add to this 'loopback path' until - if necessary - this one gets too long as well (note: length limit is -35 for this due to guid root folder)
				10. If the 'overlength path array' fills, clear it and cycle again
				11. Create the absolute path to the SFS root + the loopback path and return it
		*/
		protected virtual string GetTargetPhysicalPath(IItemData item)
		{
			var strippedItemName = PrepareItemNameForFileSystem(item.Name);

			var parentItem = GetParentSerializedItem(item);

			//  If no matches exist, throw - parent must be serialized
			if (parentItem == null) throw new InvalidOperationException("The parent item of {0} was not serialized. You cannot have a sparse serialized tree.".FormatWith(item.Path));

			// Determine if this item has any name-dupes in the source store.
			var nameDupeCandidateItems = GetChildPaths(parentItem)
				.Where(path => Path.GetFileName(path).StartsWith(strippedItemName))
				.Select(ReadItem);

			string basePath = null;

			foreach (var candidate in nameDupeCandidateItems)
			{
				if (candidate.Id == item.Id)
				{
					// item is already serialized, keep same item name
					basePath = candidate.SerializedItemId;
					break;
				}

				if (candidate.Id != item.Id && candidate.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase) && basePath == null)
				{
					// we found an item with a different ID, and the same name - so we need to escape this item's name
					basePath = string.Concat(Path.ChangeExtension(parentItem.SerializedItemId, null), Path.DirectorySeparatorChar, strippedItemName, "_", item.Id, _formatter.FileExtension);
				}
			}

			// Determine if the base-string is over - length(which would be 240 -$(Serialization.SerializationFolderPathMaxLength))
			// TODO: move the settings somewhere tests can inject?
			int maxPathLength = 240 - Settings.GetIntSetting("Serialization.SerializationFolderPathMaxLength", 80);

			if (basePath.Length < maxPathLength) return basePath;

			// Break up the path into an array. Loop over it, counting length up until you find the node that gets too long
			// Start up a new path parts list, and as you keep looping over the parts of the base string add to this 'loopback path' until - if necessary - this one gets too long as well (note: length limit is - 35 for this due to guid root folder)
			// If the 'overlength path array' fills, clear it and cycle again
			// Create the absolute path to the SFS root + the loopback path and return it
			throw new NotImplementedException();
		}

		/*
		FINDING FILE PATHS, GIVEN A VIRTUAL PATH

			1. Break apart the virtual path into components
			2. Beginning with the physical SFS root, drill down the virtual path components (use 'finding child paths, given a physical path')
				- At each level, look for all possible item names (item.yml and item_2342434.yml)
				- If multiple are present, follow ALL paths down
			3. If an expected child path is not present, stop searching down the current path
			4. Once all possible paths have been searched down, return files that matched the path
			5. Note: this means that if there are two /foo items with children named 'bar', and you get /foo/bar by path, you'll get BOTH bar items even though different parents. Paths match, bro. Sitecore API would pick one of the foos and give only one child, fwiw.
		*/
		protected virtual string[] GetPhysicalFilePathsForVirtualPath(string virtualPath)
		{
			var pathComponents = virtualPath.Trim('/').Split('/').Select(PrepareItemNameForFileSystem);

			var parentPaths = new List<string>();
			parentPaths.Add(_physicalRootPath);

			foreach (var pathComponent in pathComponents)
			{
				// copy the parent paths; we process all for this path level and add next path level candidates to the list
				var startingParentPathsArray = parentPaths.ToArray();
				parentPaths.Clear();

				foreach (var parentPath in startingParentPathsArray)
				{
					// get children of all parent paths which match the expected path component
					// e.g. if component is "foo" find "c:\bar\foo.yml" and "c:\bar\foo_0xA9f4.yml"
					parentPaths.AddRange(
						GetChildPaths(ReadItem(parentPath))
						.Where(childPath => Path.GetFileName(childPath).StartsWith(pathComponent))
					);
				}
			}

			// once we get here, parentPaths should consist of all paths down the tree that satisfy the virtual path we got passed
			return parentPaths.ToArray();
		}

		/*
		FINDING CHILD PATHS, GIVEN A VIRTUAL PATH AND ID

			1. Determine the physical path of the item to get children for (use 'finding file paths, given a virtual path'), compare matches against ID
			2. Determine if Path.GetFileNameWithoutExtension(filename) exists as a directory in the physical path
			3. If it does, get all children of that directory that are both files and have the expected serialized item extension from the formatter
			4. Read the parent item file and get the item ID. Check in the SFS root for a folder named that ID (a loopback with children of the item whose names were too long to fit)
				- If the folder exists, add all children of that directory that are both files and have the expected serialized item extension from the formatter
			5. Note: unlike searching by path, this guarantees ONLY children of the correct item if multiple same named items are present
		*/
		protected virtual string[] GetChildPaths(IItemData item)
		{
			var localPath = ConvertGlobalPathToTreePath(item.Path);

			var serializedItem = GetItemForVirtualPath(localPath, item.Id);

			if (serializedItem == null) throw new InvalidOperationException("Item {0} does not exist on disk.".FormatWith(item.Path));

			IEnumerable<string> children = Directory.GetFiles(Path.GetFileName(serializedItem.SerializedItemId), "*" + _formatter.FileExtension);

			var shortPath = Path.Combine(_physicalRootPath, item.Id.ToString());

			if (Directory.Exists(shortPath))
				children = children.Concat(Directory.GetFiles(shortPath, "*" + _formatter.FileExtension));

			return children.ToArray();
		}

		protected virtual string PrepareItemNameForFileSystem(string name)
		{
			return Regex.Replace(name, @"[%\$\\/:]+", "-");
		}

		protected virtual IItemData GetItemForVirtualPath(string virtualPath, Guid expectedItemId)
		{
			return GetPhysicalFilePathsForVirtualPath(virtualPath)
				.Select(ReadItem)
				.FirstOrDefault(candidateItem => candidateItem.Id == expectedItemId);
		}

		protected virtual IList<IItemData> GetDescendants(IItemData root)
		{
			var localPath = ConvertGlobalPathToTreePath(root.Path);
			var itemToRemove = GetItemForVirtualPath(localPath, root.Id);

			if (itemToRemove == null) return null;

			var descendants = new List<IItemData>();

			var childQueue = new Queue<IItemData>();
			childQueue.Enqueue(root);

			while (childQueue.Count > 0)
			{
				var parent = childQueue.Dequeue();

				var children = GetChildPaths(parent).Select(ReadItem).ToArray();

				descendants.AddRange(children);
				foreach (var item in children)
					childQueue.Enqueue(item);
			}

			return descendants;
		}

		protected virtual IItemData GetParentSerializedItem(IItemData item)
		{
			var localPath = ConvertGlobalPathToTreePath(item.Path);

			// Start by using "finding file paths, given a virtual path" on the parent path of the item
			var parentVirtualPath = item.Path.Substring(0, localPath.LastIndexOf('/'));

			var parentPhysicalPaths = GetPhysicalFilePathsForVirtualPath(parentVirtualPath);

			if (parentPhysicalPaths.Length == 0) return null;

			// If multiple parent path matches exist, narrow them by the parent ID of the item - if no matches, throw
			if (parentPhysicalPaths.Length > 1)
			{
				// find the expected parent's physical path
				var parentItem = GetItemForVirtualPath(localPath, item.Id);

				if (parentItem == null) return null;

				return parentItem;
			}

			return ReadItem(parentPhysicalPaths[0]);
		}
	}
}
