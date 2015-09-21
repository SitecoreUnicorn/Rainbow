using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Rainbow.Formatting;
using Rainbow.Model;
using Rainbow.SourceControl;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Rainbow.Storage
{
	/// <summary>
	/// SFS data store stores serialized items on the file system.
	/// Items are organized into one or more subtrees. Each tree must be solid (e.g. if a child is written all parents must also be written)
	/// </summary>
	public class SerializationFileSystemDataStore : IDataStore, IDocumentable, IDisposable
	{
		protected readonly string PhysicalRootPath;
		private readonly bool _useDataCache;
		private readonly ITreeRootFactory _rootFactory;
		private readonly ISerializationFormatter _formatter;
		protected readonly List<SerializationFileSystemTree> Trees;
		protected readonly List<Action<IItemMetadata, string>> ChangeWatchers = new List<Action<IItemMetadata, string>>();
		private readonly ISourceControlManager _sourceControlManager;
		
		public SerializationFileSystemDataStore(string physicalRootPath, bool useDataCache, ITreeRootFactory rootFactory, ISerializationFormatter formatter, ISourceControlSync sourceControlSync)
		{
			Assert.ArgumentNotNullOrEmpty(physicalRootPath, "rootPath");
			Assert.ArgumentNotNull(formatter, "formatter");
			Assert.ArgumentNotNull(rootFactory, "rootFactory");

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			PhysicalRootPath = InitializeRootPath(physicalRootPath);

			_useDataCache = useDataCache;
			_rootFactory = rootFactory;
			_formatter = formatter;
			_sourceControlManager = new SourceControlManager(sourceControlSync);
			_formatter.ParentDataStore = this;
			
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			Trees = InitializeTrees();
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
			var oldPathTree = GetTreeForPath(oldPath, itemWithFinalPath.DatabaseName);

			// remove existing items if they exist
			if (oldPathTree != null)
			{
				var oldItem = oldPathTree.GetItemsByPath(oldPath).FirstOrDefault(item => item.Id == itemWithFinalPath.Id);
				if (oldItem != null) oldPathTree.Remove(oldItem);
			}

			var newPathTree = GetTreeForPath(itemWithFinalPath.Path, itemWithFinalPath.DatabaseName);

			// add new tree, if it's included (if it's moving to a non included path we simply delete it and are done)
			if (newPathTree != null)
			{
				var saveQueue = new Queue<IItemData>();
				saveQueue.Enqueue(itemWithFinalPath);

				while (saveQueue.Count > 0)
				{
					var parent = saveQueue.Dequeue();

					Save(parent);

					var children = parent.GetChildren();

					foreach (var child in children)
					{
						saveQueue.Enqueue(child);
					}
				}
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

				if (result != null)
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
			if (!Directory.Exists(PhysicalRootPath)) return;

			if (!_sourceControlManager.AllowFileSystemClear)
			{
				throw new InvalidOperationException("Cannot clear the local file system. The serialization tree must first be cleared in source control before continuing.");
			}

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
				rootPath = HostingEnvironment.MapPath("~/") + rootPath.Substring(1);
			}

			if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

			return rootPath;
		}

		protected virtual List<SerializationFileSystemTree> InitializeTrees()
		{
			return _rootFactory.CreateTreeRoots().Select(CreateTree).ToList();
		}

		protected virtual SerializationFileSystemTree GetTreeForPath(string path, string database)
		{
			var trees = Trees.Where(tree => tree.DatabaseName.Equals(database, StringComparison.OrdinalIgnoreCase) && tree.ContainsPath(path)).ToArray();

			if (trees.Length == 0)
			{
				return null;
			}

			if (trees.Length > 1) throw new InvalidOperationException("The trees {0} contained the global path {1} - overlapping trees are not allowed.".FormatWith(string.Join(", ", trees.Select(tree => tree.Name)), path));

			return trees[0];
		}

		protected virtual SerializationFileSystemTree CreateTree(TreeRoot root)
		{
			var tree = new SerializationFileSystemTree(root.Name, root.Path, root.DatabaseName, Path.Combine(PhysicalRootPath, root.Name), _formatter, _useDataCache, _sourceControlManager.SourceControlSync);
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



		public virtual string FriendlyName { get { return "Serialization File System Data Store"; } }
		public virtual string Description { get { return "Stores serialized items on disk using the SFS tree format, where each root is a separate tree."; } }
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
				foreach(var tree in Trees) tree.Dispose();
			}
		}
	}
}
