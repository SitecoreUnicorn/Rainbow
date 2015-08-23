using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rainbow.Formatting;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.StringExtensions;

namespace Rainbow.Storage
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
	public class SerializationFileSystemTree
	{
		private readonly string _globalRootItemPath;
		protected readonly string PhysicalRootPath;
		private readonly ISerializationFormatter _formatter;
		private readonly Dictionary<Guid, IItemMetadata> _idCache = new Dictionary<Guid, IItemMetadata>();
		private readonly FsCache<IItemData> _dataCache;
		private readonly FsCache<IItemMetadata> _metadataCache = new FsCache<IItemMetadata>(true);

		private bool _configuredForFastReads = false;
		private readonly object _fastReadConfigurationLock = new object();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">A name for this tree, for your reference</param>
		/// <param name="globalRootItemPath">The 'global' path where this tree is rooted. For example, if this was '/sitecore/content', the root item in this tree would be 'content'</param>
		/// <param name="databaseName">Name of the database the items in this tree are from. This is for your reference and help when resolving this tree as a destination, and is not directly used.</param>
		/// <param name="physicalRootPath">The physical root path to write items in this tree to. Will be created if it does not exist.</param>
		/// <param name="formatter">The formatter to use when reading or writing items to disk</param>
		/// <param name="useDataCache">Whether to cache items read in memory for later rapid retrieval. Great for small trees, or if you have plenty of RAM. Bad for media trees :)</param>
		public SerializationFileSystemTree(string name, string globalRootItemPath, string databaseName, string physicalRootPath, ISerializationFormatter formatter, bool useDataCache)
		{
			Assert.ArgumentNotNullOrEmpty(globalRootItemPath, "globalRootItemPath");
			Assert.ArgumentNotNullOrEmpty(databaseName, "databaseName");
			Assert.ArgumentNotNullOrEmpty(physicalRootPath, "physicalRootPath");
			Assert.ArgumentNotNull(formatter, "formatter");
			Assert.IsTrue(globalRootItemPath.StartsWith("/"), "The global root item path must start with '/', e.g. '/sitecore' or '/sitecore/content'");
			Assert.IsTrue(globalRootItemPath.Length > 1, "The global root item path cannot be '/' - there is no root item. You probably mean '/sitecore'.");

			_globalRootItemPath = globalRootItemPath.TrimEnd('/');
			PhysicalRootPath = physicalRootPath;
			_formatter = formatter;
			_dataCache = new FsCache<IItemData>(useDataCache);
			Name = name;
			DatabaseName = databaseName;

			if (!Directory.Exists(PhysicalRootPath)) Directory.CreateDirectory(PhysicalRootPath);
		}

		public string DatabaseName { get; private set; }
		[ExcludeFromCodeCoverage]
		public string Name { get; private set; }

		public bool ContainsPath(string globalPath)
		{
			// test that the path is under the global root
			return globalPath.StartsWith(_globalRootItemPath, StringComparison.OrdinalIgnoreCase);
		}

		public IItemData GetRootItem()
		{
			var rootItem = Directory.GetFiles(PhysicalRootPath, "*" + _formatter.FileExtension);

			if (rootItem.Length == 0) return null;

			if (rootItem.Length > 1)
				throw new InvalidOperationException("Found multiple root items in " + PhysicalRootPath + "! This is not valid: a tree may only have one root inode.");

			return ReadItem(rootItem[0]);
		}

		public IEnumerable<IItemData> GetItemsByPath(string globalPath)
		{
			Assert.ArgumentNotNullOrEmpty(globalPath, "globalPath");

			var localPath = ConvertGlobalVirtualPathToTreeVirtualPath(globalPath);

			return GetPhysicalFilePathsForVirtualPath(localPath).Select(ReadItem).Where(item => item != null && item.Path.Equals(globalPath, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Gets an item from the tree by ID.
		/// Note: the first call to this method ensures that the tree is configured for high speed reading by ID,
		/// which fills the metadata cache with all instances from disk. Reading by ID is not recommended for very large datasets,
		/// but is quite good with smaller datasets of 1000-2000 items.
		/// </summary>
		public IItemData GetItemById(Guid id)
		{
			EnsureConfiguredForFastReads();

			IItemMetadata cached = GetFromMetadataCache(id);
			if (cached == null) return null;

			return ReadItem(cached.SerializedItemId);
		}


		public IEnumerable<IItemData> GetChildren(IItemMetadata parentItem)
		{
			Assert.ArgumentNotNull(parentItem, "parentItem");

			return GetChildPaths(parentItem).Select(ReadItem);
		}

		public IEnumerable<IItemMetadata> GetChildrenMetadata(IItemMetadata parentItem)
		{
			Assert.ArgumentNotNull(parentItem, "parentItem");

			return GetChildPaths(parentItem).Select(ReadItemMetadata);
		}

		/*
		REMOVING ITEMS, GIVEN A VIRTUAL PATH AND ID

			1. Determine the physical path of the item to get children for (use 'finding file paths, given a virtual path'), compare matches against ID
			2. Use 'finding child paths, given a physical path' recursively to find all descendant items including through loopbacks
			3. Starting at the deepest paths, begin deleting item files and - if present - children subfolders, until all are gone
		*/

		public bool Remove(IItemData item)
		{
			Assert.ArgumentNotNull(item, "item");

			IItemMetadata itemToRemove = GetItemForGlobalPath(item.Path, item.Id);

			if (itemToRemove == null) return false;

			var descendants = GetDescendants(item, true).Concat(new[] { itemToRemove });

			foreach (var descendant in descendants.OrderByDescending(desc => desc.Path))
			{
				lock (FileUtil.GetFileLock(descendant.SerializedItemId))
				{
					File.Delete(descendant.SerializedItemId);

					var childrenDirectory = Path.ChangeExtension(descendant.SerializedItemId, null);

					if (Directory.Exists(childrenDirectory)) Directory.Delete(childrenDirectory, true);

					var shortChildrenDirectory = Path.Combine(PhysicalRootPath, descendant.Id.ToString());
					if (Directory.Exists(shortChildrenDirectory)) Directory.Delete(shortChildrenDirectory);
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
			Assert.ArgumentNotNull(item, "item");

			var storagePath = GetTargetPhysicalPath(item);

			WriteItem(item, storagePath);
		}

		protected virtual IItemData ReadItem(string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");

			return _dataCache.GetValue(path, fileInfo =>
			{
				using (var reader = fileInfo.OpenRead())
				{
					var readItem = _formatter.ReadSerializedItem(reader, path);
					readItem.DatabaseName = DatabaseName;

					AddToMetadataCache(readItem);

					return readItem;
				}
			});
		}

		protected virtual IItemMetadata ReadItemMetadata(string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");

			return _metadataCache.GetValue(path, fileInfo =>
			{
				using (var reader = fileInfo.OpenRead())
				{
					var readItem = _formatter.ReadSerializedItemMetadata(reader, path);

					return readItem;
				}
			});
		}

		protected virtual void WriteItem(IItemData item, string path)
		{
			Assert.ArgumentNotNull(item, "item");
			Assert.ArgumentNotNullOrEmpty(path, "path");

			var proxiedItem = new ProxyItem(item);

			lock (FileUtil.GetFileLock(path))
			{
				try
				{
					var directory = Path.GetDirectoryName(path);
					if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

					using (var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						_formatter.WriteSerializedItem(proxiedItem, writer);
					}
				}
				catch
				{
					if (File.Exists(path)) File.Delete(path);
					throw;
				}
			}

			AddToMetadataCache(item);
			_dataCache.AddOrUpdate(path, proxiedItem);
		}

		protected virtual string ConvertGlobalVirtualPathToTreeVirtualPath(string globalPath)
		{
			Assert.ArgumentNotNullOrEmpty(globalPath, "globalPath");

			if (!globalPath.StartsWith(_globalRootItemPath, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("The global path {0} was not rooted under the local item root path {1}. This means you tried to put an item where it didn't belong.".FormatWith(globalPath, _globalRootItemPath));

			// we want to preserve the last segment in the global path because the root virtual path is considered to contain that
			// e.g. if the global path is "/sitecore/templates" we want the converted path to start with /templates - e.g. /sitecore/templates/foo -> /templates/foo

			int globalPathClipIndex = _globalRootItemPath.LastIndexOf('/');
			return globalPath.Substring(globalPathClipIndex);
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
				8. If it's over length create a folder named the parent ID in the root folder, and put it there
		*/

		protected virtual string GetTargetPhysicalPath(IItemData item)
		{
			Assert.ArgumentNotNull(item, "item");

			var strippedItemName = PrepareItemNameForFileSystem(item.Name);

			// if the item is the root item in our tree, we return the root physical path
			var localPath = ConvertGlobalVirtualPathToTreeVirtualPath(item.Path);
			if (localPath.LastIndexOf('/') == 0)
				return Path.Combine(PhysicalRootPath, strippedItemName + _formatter.FileExtension);
			// if it's the root item, well yeah it won't have a parent. And we know the right path too :)

			var parentItem = GetParentSerializedItem(item);

			//  If no matches exist, and the item isn't the root item, throw - parent must be serialized
			if (parentItem == null)
			{
				throw new InvalidOperationException("The parent item of {0} was not serialized. You cannot have a sparse serialized tree.".FormatWith(item.Path));
			}

			// Determine if this item has any name-dupes in the source store.
			var nameDupeCandidateItems = GetChildPaths(parentItem)
				.Where(path => Path.GetFileName(path).StartsWith(strippedItemName, StringComparison.OrdinalIgnoreCase))
				.Select(ReadItemMetadata);

			// the base path is the path the item would be written to on the filesystem *if there were no length limitations*. Note that this is why we avoid using Path.X() on the base path, because those validate path lengths.
			string basePath = null;

			foreach (var candidate in nameDupeCandidateItems)
			{
				if (candidate.Id == item.Id)
				{
					// item is already serialized, keep same item name
					basePath = candidate.SerializedItemId;
					break;
				}

				if (candidate.Id != item.Id && basePath == null)
				{
					// we found an item with a different ID, and the same name - so we need to escape this item's name
					basePath = string.Concat(Path.ChangeExtension(parentItem.SerializedItemId, null), Path.DirectorySeparatorChar, strippedItemName, "_", item.Id, _formatter.FileExtension);
				}
			}

			// no dupes or existing item found - create default base path
			if (basePath == null)
				basePath = string.Concat(Path.ChangeExtension(parentItem.SerializedItemId, null), Path.DirectorySeparatorChar, strippedItemName, _formatter.FileExtension);

			// Determine if the relative base-string is over - length(which would be 240 - $(Serialization.SerializationFolderPathMaxLength))
			string relativeBasePath = basePath.Substring(PhysicalRootPath.Length);
			int maxPathLength = MaxRelativePathLength;

			// path not over length = return it and we're done here
			if (relativeBasePath.Length < maxPathLength) return basePath;

			// ok we have a path that will be too long for Windows once we combine the parent's physical path with the item's name (and/or name deduping ID)
			// since we know it won't fit in the parent, we create a folder in the root named the parent's ID, and drop that as the item's base path
			// e.g. c:\foo\long\path\foo.yml > c:\foo\[id-of-path]\foo.yml
			return Path.Combine(PhysicalRootPath, parentItem.Id.ToString(), basePath.Substring(basePath.LastIndexOf(Path.DirectorySeparatorChar) + 1));
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
			Assert.ArgumentNotNullOrEmpty(virtualPath, "virtualPath");

			var pathComponents = virtualPath.Trim('/').Split('/').Select(PrepareItemNameForFileSystem).ToArray();

			var parentPaths = new List<string>();
			parentPaths.Add(Path.Combine(PhysicalRootPath, pathComponents[0] + _formatter.FileExtension));

			foreach (var pathComponent in pathComponents.Skip(1))
			{
				// copy the parent paths; we process all for this path level and add next path level candidates to the list
				var startingParentPathsArray = parentPaths.ToArray();
				parentPaths.Clear();

				foreach (var parentPath in startingParentPathsArray)
				{
					if (!File.Exists(parentPath)) continue;

					// get children of all parent paths which match the expected path component
					// e.g. if component is "foo" find "c:\bar\foo.yml" and "c:\bar\foo_0xA9f4.yml"
					parentPaths.AddRange(
						GetChildPaths(ReadItemMetadata(parentPath))
							.Where(childPath => Path.GetFileName(childPath).StartsWith(pathComponent, StringComparison.OrdinalIgnoreCase))
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

		protected virtual string[] GetChildPaths(IItemMetadata item)
		{
			Assert.ArgumentNotNull(item, "item");

			IItemMetadata serializedItem = GetItemForGlobalPath(item.Path, item.Id);

			if (serializedItem == null)
				throw new InvalidOperationException("Item {0} does not exist on disk.".FormatWith(item.Path));

			IEnumerable<FileData> children = Enumerable.Empty<FileData>();

			var childrenPath = Path.ChangeExtension(serializedItem.SerializedItemId, null);

			if (Directory.Exists(childrenPath))
			{
				children = FastDirectoryEnumerator.GetFiles(childrenPath, "*" + _formatter.FileExtension, SearchOption.TopDirectoryOnly);
			}

			var shortPath = Path.Combine(PhysicalRootPath, item.Id.ToString());

			if (Directory.Exists(shortPath))
				children = children.Concat(FastDirectoryEnumerator.GetFiles(shortPath, "*" + _formatter.FileExtension, SearchOption.TopDirectoryOnly));

			return children.Select(result => result.Path).ToArray();
		}

		protected virtual string PrepareItemNameForFileSystem(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, "name");

			var validifiedName = Regex.Replace(name, @"[%\$\\/:\*\?<>\|""]+", "_", RegexOptions.Compiled);

			if (validifiedName.Length > MaxItemNameLengthBeforeTruncation)
				validifiedName = validifiedName.Substring(0, MaxItemNameLengthBeforeTruncation);

			// if the name ends with a space that can cause ambiguous results (e.g. "Multilist" and "Multilist "); Win32 considers directories with trailing spaces as the same as without, so we end it with underscore instead
			if (validifiedName[validifiedName.Length - 1] == ' ') validifiedName = validifiedName.Substring(0, validifiedName.Length - 1) + "_";

			return validifiedName;
		}

		protected virtual IItemMetadata GetItemForGlobalPath(string globalPath, Guid expectedItemId)
		{
			Assert.ArgumentNotNullOrEmpty(globalPath, "virtualPath");

			var localPath = ConvertGlobalVirtualPathToTreeVirtualPath(globalPath);

			IItemMetadata cached = GetFromMetadataCache(expectedItemId);
			if (cached != null && globalPath.Equals(cached.Path, StringComparison.OrdinalIgnoreCase)) return cached;

			var result = GetPhysicalFilePathsForVirtualPath(localPath)
				.Select(ReadItemMetadata)
				.FirstOrDefault(candidateItem => candidateItem.Id == expectedItemId);

			if (result == null) return null;

			IItemMetadata temp = GetFromMetadataCache(expectedItemId);
			if (temp != null && temp.SerializedItemId != result.SerializedItemId) throw new InvalidOperationException("The item with ID {0} has duplicate item files serialized ({1}, {2}). Please remove the incorrect one and try again.".FormatWith(result.Id, _idCache[result.Id].SerializedItemId, result.SerializedItemId));

			AddToMetadataCache(result);

			return result;
		}

		protected virtual IList<IItemMetadata> GetDescendants(IItemData root, bool ignoreReadErrors)
		{
			Assert.ArgumentNotNull(root, "root");

			var descendants = new List<IItemMetadata>();

			var childQueue = new Queue<IItemMetadata>();
			childQueue.Enqueue(root);

			while (childQueue.Count > 0)
			{
				var parent = childQueue.Dequeue();

				// add current item to descendant results
				if (parent.Id != root.Id)
				{
					descendants.Add(parent);
				}

				var children = GetChildPaths(parent);
				
				foreach(var physicalPath in children)
				{
					try
					{
						var child = ReadItemMetadata(physicalPath);
						if (child != null) childQueue.Enqueue(child);
					}
					catch (Exception)
					{
						if (ignoreReadErrors) continue;

						throw;
					}
				}
			}

			return descendants;
		}

		protected virtual IItemMetadata GetParentSerializedItem(IItemMetadata item)
		{
			Assert.ArgumentNotNull(item, "item");

			var localPath = ConvertGlobalVirtualPathToTreeVirtualPath(item.Path);

			// Start by using "finding file paths, given a virtual path" on the parent path of the item
			var parentVirtualPath = localPath.Substring(0, localPath.LastIndexOf('/'));

			if (parentVirtualPath == string.Empty) return null;

			var parentPhysicalPaths = GetPhysicalFilePathsForVirtualPath(parentVirtualPath);

			if (parentPhysicalPaths.Length == 0) return null;

			// If multiple parent path matches exist, narrow them by the parent ID of the item - if no matches, throw
			if (parentPhysicalPaths.Length > 1)
			{
				// find the expected parent's physical path
				var parentItem = parentPhysicalPaths.Select(ReadItemMetadata).FirstOrDefault(parentCandiate => parentCandiate.Id == item.ParentId);

				if (parentItem == null) return null;

				return parentItem;
			}

			return ReadItemMetadata(parentPhysicalPaths[0]);
		}

		private int? _maxRelativePathLength;
		/// <summary>
		/// This is the 'effective' max relative physical path length before we start having to use loopback paths.
		/// This is usually (Windows Path Max) - (Constant), where the constant is the maximum expected physical path length
		/// to the SFS tree's root directory.
		/// </summary>
		protected virtual int MaxRelativePathLength
		{
			get
			{
				if (_maxRelativePathLength == null)
				{
					const int windowsMaxPathLength = 240; // 260 - sundry directory chars, separators, file extension allowance, etc
					int expectedPhysicalPathMaxConstant = Settings.GetIntSetting("Rainbow.SFS.SerializationFolderPathMaxLength", 80);

					if (PhysicalRootPath.Length > expectedPhysicalPathMaxConstant)
						throw new InvalidOperationException("The physical root path of this SFS tree, {0}, is longer than the configured max base path length {1}. If the tree contains any loopback paths, unexpected behavior may occur. You should increase the Serialization.SerializationFolderPathMaxLength setting to greater than {2} and perform a reserialization from a master content database."
								.FormatWith(PhysicalRootPath, expectedPhysicalPathMaxConstant, PhysicalRootPath.Length));

					_maxRelativePathLength = windowsMaxPathLength - expectedPhysicalPathMaxConstant;
				}

				return _maxRelativePathLength.Value;
			}
		}

		private int? _maxItemNameLength;
		/// <summary>
		/// Sitecore item names can become so long that they will not fit on the filesystem without hitting the max path length.
		/// This setting controls when Rainbow truncates item file names that are extremely long so they will fit on the filesystem.
		/// The value must be less than MAX_PATH - SerializationFolderPathMaxLength - Length of GUID - length of file extension.
		/// </summary>
		protected virtual int MaxItemNameLengthBeforeTruncation
		{
			get
			{
				if (_maxItemNameLength == null)
				{
					var configSetting = Settings.GetIntSetting("Rainbow.SFS.MaxItemNameLengthBeforeTruncation", 100);
					var maxLength = MaxRelativePathLength - Guid.Empty.ToString().Length - _formatter.FileExtension.Length;
					if (configSetting > maxLength)
						throw new InvalidOperationException("The MaxItemNameLengthBeforeTruncation setting ({0}) is too long given the SerializationFolderPathMaxLength. Reduce the max name length to at or below {1}.".FormatWith(configSetting, maxLength));

					_maxItemNameLength = configSetting;
				}

				return _maxItemNameLength.Value;
			}
		}

		protected void ClearCaches()
		{
			_idCache.Clear();
			_metadataCache.Clear();
			_dataCache.Clear();
		}

		protected void AddToMetadataCache(IItemMetadata metadata)
		{
			var cachedValue = new WrittenItemMetadata(metadata.Id, metadata.ParentId, metadata.TemplateId, metadata.Path, metadata.SerializedItemId);
			_idCache[metadata.Id] = cachedValue;
			_metadataCache.AddOrUpdate(metadata.SerializedItemId, metadata);
		}

		protected IItemMetadata GetFromMetadataCache(Guid itemId)
		{
			IItemMetadata cached;
			if (_idCache.TryGetValue(itemId, out cached) && File.Exists(cached.SerializedItemId)) return cached;

			return null;
		}

		protected IItemMetadata GetFromMetadataCache(string physicalPath)
		{
			var metadata = _metadataCache.GetValue(physicalPath);

			if (metadata != null) return metadata;

			return _dataCache.GetValue(physicalPath);
		}

		/// <summary>
		/// Configures the tree to enable fast reads of items by ID or template ID,
		/// by preloading the whole metadata on disk into cache at once and then
		/// watching for metadata changes with a filesystem watcher to update the cache.
		/// 
		/// This enables us to rapidly say "this ID is not in this tree," which is an
		/// essential component of a performant data provider read implementation.
		/// </summary>
		protected virtual void EnsureConfiguredForFastReads()
		{
			if (_configuredForFastReads) return;

			lock (_fastReadConfigurationLock)
			{
				if (_configuredForFastReads) return;

				// getting descendants of the root item will populate the metadata cache with the entirety of what's on disk
				var root = GetRootItem();
				if (root != null)
				{
					GetDescendants(root, false);
				}

				// TODO: watch for added or deleted files with a filesystem watcher
				// note: we don't care about changed files, because FSCache checks for a later mod date already for cache invalidation

				_configuredForFastReads = true;
			}
		}

		protected class WrittenItemMetadata : IItemMetadata
		{
			public WrittenItemMetadata(Guid id, Guid parentId, Guid templateId, string path, string serializedItemId)
			{
				Id = id;
				ParentId = parentId;
				TemplateId = templateId;
				Path = path;
				SerializedItemId = serializedItemId;
			}

			public Guid Id { get; private set; }
			public Guid ParentId { get; private set; }
			public Guid TemplateId { get; private set; }
			public string Path { get; private set; }
			public string SerializedItemId { get; private set; }
		}
	}
}
