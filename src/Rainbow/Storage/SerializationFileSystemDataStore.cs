using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Rainbow.Formatting;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Storage
{
	/// <summary>
	/// SFS data store stores serialized items on the file system.
	/// Items are organized into one or more subtrees. Each tree must be solid (e.g. if a child is written all parents must also be written)
	/// </summary>
	public class SerializationFileSystemDataStore : ISnapshotCapableDataStore, IDocumentable, IDisposable
	{
		protected readonly string PhysicalRootPath;
		private readonly bool _useDataCache;
		private readonly ITreeRootFactory _rootFactory;
		protected readonly List<SerializationFileSystemTree> Trees;
		protected readonly List<Action<IItemMetadata, string>> ChangeWatchers = new List<Action<IItemMetadata, string>>();
		private readonly ISerializationFormatter _formatter;

		public SerializationFileSystemDataStore(string physicalRootPath, bool useDataCache, ITreeRootFactory rootFactory, ISerializationFormatter formatter)
		{
			Assert.ArgumentNotNullOrEmpty(physicalRootPath, nameof(physicalRootPath));
			Assert.ArgumentNotNull(formatter, nameof(formatter));
			Assert.ArgumentNotNull(rootFactory, nameof(rootFactory));

			_useDataCache = useDataCache;
			_rootFactory = rootFactory;
			_formatter = formatter;
			_formatter.ParentDataStore = this;

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			PhysicalRootPath = InitializeRootPath(physicalRootPath);

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			Trees = InitializeTrees(_formatter, useDataCache);
		}

		public virtual IEnumerable<IItemData> GetSnapshot()
		{
			return Trees.SelectMany(tree => tree.GetSnapshot());
		}

		public virtual void Save(IItemData item)
		{
			var tree = GetTreeForPath(item.Path, item.DatabaseName);

			if (tree == null) throw new InvalidOperationException("No trees contained the global path " + item.Path);

			tree.Save(item);
		}

		/*
		Moving an item involves:
		- Get the item from the tree, delete
		- If the new path is included in ANY tree
			- Get the serialized parent at the destination
			- Get the moved tree from Sitecore, save whole tree (NOTE: we had to update to final path in DP - what about children are those with old or new path?)

		Renaming an item involves:
		- The same thing as moving an item
		*/
		public virtual void MoveOrRenameItem(IItemData itemWithFinalPath, string oldPath)
		{
			// GET EXISTING ITEM WE'RE MOVING + DESCENDANT PATHS
			var oldPathTree = GetTreeForPath(oldPath, itemWithFinalPath.DatabaseName);
			Dictionary<string, IItemMetadata> oldPathItemAndDescendants;

			var oldPathItem = oldPathTree?.GetItemsByPath(oldPath).FirstOrDefault(item => item.Id == itemWithFinalPath.Id);
			if (oldPathItem != null)
			{
				oldPathItemAndDescendants = oldPathTree.GetDescendants(oldPathItem, false).ToDictionary(item => item.SerializedItemId);
				oldPathItemAndDescendants.Add(oldPathItem.SerializedItemId, oldPathItem);
			}
			else oldPathItemAndDescendants = new Dictionary<string, IItemMetadata>();

			// WRITE THE NEW MOVED/RENAMED ITEMS TO THE TREE (note: delete goes last because with TpSync we need the old items to read from)
			var newPathTree = GetTreeForPath(itemWithFinalPath.Path, itemWithFinalPath.DatabaseName);

			// force consistency of parent IDs and paths among child items before we serialize them
			var rebasedPathItem = new PathRebasingProxyItem(itemWithFinalPath);

			// add new tree, if it's included (if it's moving to a non included path we simply delete it and are done)
			if (newPathTree != null)
			{
				var saveQueue = new Queue<IItemData>();
				saveQueue.Enqueue(rebasedPathItem);

				while (saveQueue.Count > 0)
				{
					var parent = saveQueue.Dequeue();

					var tree = GetTreeForPath(parent.Path, parent.DatabaseName);

					if (tree == null) throw new InvalidOperationException("No trees contained the global path " + parent.Path);

					using (new SfsDuplicateIdCheckingDisabler())
					{
						var savedPath = tree.Save(parent);

						// if we saved an item that was a former child of the item we want to keep it when we're doing deletions
						if (oldPathItemAndDescendants.ContainsKey(savedPath)) oldPathItemAndDescendants.Remove(savedPath);

						var children = parent.GetChildren();

						foreach (var child in children)
						{
							saveQueue.Enqueue(child);
						}
					}
				}
			}

			// in case an item was renamed by case or someone calls rename without renaming, we don't want to delete anything
			// 'cause that'd just delete the item, not move it :)
			if (oldPath.Equals(itemWithFinalPath.Path, StringComparison.OrdinalIgnoreCase)) return;

			// REMOVE EXISTING ITEMS (if any)
			// (excluding any items that we wrote to during the save phase above, e.g. loopback path items may not change during a move)
			if (oldPathTree != null)
			{
				var oldItems = oldPathItemAndDescendants
					.Select(key => key.Value)
					.OrderByDescending(item => item.Path)
					.ToArray();

				foreach (var item in oldItems) oldPathTree.RemoveWithoutChildren(item);
			}
		}

		public virtual IEnumerable<IItemData> GetByPath(string path, string database)
		{
			var tree = GetTreeForPath(path, database);

			if (tree == null) return Enumerable.Empty<IItemData>();

			return tree.GetItemsByPath(path);
		}

		public virtual IItemData GetByPathAndId(string path, Guid id, string database)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentCondition(id != default(Guid), "id", "The item ID must not be the null guid. Use GetByPath() if you don't know the ID.");

			var items = GetByPath(path, database).ToArray();

			return items.FirstOrDefault(item => item.Id == id);
		}

		public virtual IItemData GetById(Guid id, string database)
		{
			foreach (var tree in Trees)
			{
				var result = tree.GetItemById(id);

				if (result != null && result.DatabaseName.Equals(database))
				{
					return result;
				}
			}

			return null;
		}

		public virtual IEnumerable<IItemMetadata> GetMetadataByTemplateId(Guid templateId, string database)
		{
			return Trees.Select(tree => tree.GetRootItem())
				.Where(root => root != null)
				.AsParallel()
				.SelectMany(tree => FilterDescendantsAndSelf(tree, item => item.TemplateId == templateId));
		}

		public virtual IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			var tree = GetTreeForPath(parentItem.Path, parentItem.DatabaseName);

			if (tree == null) throw new InvalidOperationException("No trees contained the global path " + parentItem.Path);

			return tree.GetChildren(parentItem);
		}

		public virtual void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			// TODO: consistency check
			throw new NotImplementedException();
		}

		public virtual void ResetTemplateEngine()
		{
			// do nothing, the YAML serializer has no template engine
		}

		public virtual bool Remove(IItemData item)
		{
			var tree = GetTreeForPath(item.Path, item.DatabaseName);

			if (tree == null) return false;

			return tree.Remove(item);
		}

		public virtual void RegisterForChanges(Action<IItemMetadata, string> actionOnChange)
		{
			ChangeWatchers.Add(actionOnChange);
		}

		public virtual void Clear()
		{
			// since we're tearing everything down we dispose all existing trees, watchers, etc and start over
			foreach (var tree in Trees) tree.Dispose();

			Trees.Clear();

			ActionRetryer.Perform(ClearAllFiles);

			// bring the trees back up, which will reestablish watchers and such
			Trees.AddRange(InitializeTrees(_formatter, _useDataCache));
		}

		protected virtual void ClearAllFiles()
		{
			// drop all existing files and directories
			if (!Directory.Exists(PhysicalRootPath)) return;
			var children = Directory.GetDirectories(PhysicalRootPath);

			foreach (var child in children)
			{
				Directory.Delete(child, true);
			}

			var files = Directory.GetFiles(PhysicalRootPath);

			foreach (var file in files)
			{
				File.Delete(file);
			}
		}

		protected virtual string InitializeRootPath(string rootPath)
		{
			if (rootPath.StartsWith("~") || rootPath.StartsWith("/"))
			{
				var cleanRootPath = rootPath.TrimStart('~', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				cleanRootPath = cleanRootPath.Replace("/", Path.DirectorySeparatorChar.ToString());

				rootPath = Path.Combine(HostingEnvironment.MapPath("~/"), cleanRootPath);
			}

			// convert root path to canonical form, so subsequent transformations can do string comparison
			// http://stackoverflow.com/questions/970911/net-remove-dots-from-the-path
			if (rootPath.Contains(".."))
				rootPath = Path.GetFullPath(rootPath);

			if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

			return rootPath;
		}

		protected virtual SerializationFileSystemTree GetTreeForPath(string path, string database)
		{
			SerializationFileSystemTree foundTree = null;
			foreach (var tree in Trees)
			{
				if (!tree.DatabaseName.Equals(database, StringComparison.OrdinalIgnoreCase)) continue;
				if (!tree.ContainsPath(path)) continue;

				if (foundTree != null)
				{
					throw new InvalidOperationException($"The trees {foundTree.Name} and {tree.Name} both contained the global path {path} - overlapping trees are not allowed.");
				}

				foundTree = tree;
			}

			return foundTree;
		}

		// note: we pass in these params (formatter, datacache) so that overriding classes may get access to private vars indirectly (can't get at them otherwise because this is called from the constructor)
		protected virtual List<SerializationFileSystemTree> InitializeTrees(ISerializationFormatter formatter, bool useDataCache)
		{
			var result = new List<SerializationFileSystemTree>();
			var roots = _rootFactory.CreateTreeRoots();

			foreach (var root in roots)
			{
				result.Add(CreateTree(root, formatter, useDataCache));
			}

			return result;
		}

		// note: we pass in these params (formatter, datacache) so that overriding classes may get access to private vars indirectly (can't get at them otherwise because this is called from the constructor)
		protected virtual SerializationFileSystemTree CreateTree(TreeRoot root, ISerializationFormatter formatter, bool useDataCache)
		{
			var tree = new SerializationFileSystemTree(root.Name, root.Path, root.DatabaseName, Path.Combine(PhysicalRootPath, root.Name), formatter, useDataCache);
			tree.TreeItemChanged += metadata =>
			{
				foreach (var watcher in ChangeWatchers) watcher(metadata, tree.DatabaseName);
			};

			return tree;
		}

		protected virtual IList<IItemMetadata> FilterDescendantsAndSelf(IItemData root, Func<IItemMetadata, bool> predicate)
		{
			Assert.ArgumentNotNull(root, "root");

			var descendants = new List<IItemMetadata>();

			var childQueue = new Queue<IItemMetadata>();
			childQueue.Enqueue(root);

			while (childQueue.Count > 0)
			{
				var parent = childQueue.Dequeue();

				if (predicate(parent)) descendants.Add(parent);

				var tree = GetTreeForPath(parent.Path, root.DatabaseName);

				if (tree == null) continue;

				var children = tree.GetChildrenMetadata(parent).ToArray();

				foreach (var item in children)
					childQueue.Enqueue(item);
			}

			return descendants;
		}

		public virtual string FriendlyName => "Serialization File System Data Store";
		public virtual string Description => "Stores serialized items on disk using the SFS tree format, where each root is a separate tree.";

		public virtual KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new[]
			{
				new KeyValuePair<string, string>("Serialization formatter", DocumentationUtility.GetFriendlyName(_formatter)),
				new KeyValuePair<string, string>("Physical root path", PhysicalRootPath),
				new KeyValuePair<string, string>("Total internal SFS trees", Trees.Count.ToString())
			};
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var tree in Trees) tree.Dispose();
			}
		}
	}
}
