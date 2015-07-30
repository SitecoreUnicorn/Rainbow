using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using Rainbow.Formatting;
using Rainbow.Model;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Rainbow.Storage
{
	public class SerializationFileSystemDataStore : IDataStore
	{
		private readonly string _rootPath;
		private readonly ITreeRootFactory _rootFactory;
		private readonly ISerializationFormatter _formatter;
		protected readonly List<SerializationFileSystemTree> Trees;

		public SerializationFileSystemDataStore(string rootPath, ITreeRootFactory rootFactory, ISerializationFormatter formatter)
		{
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");
			Assert.ArgumentNotNull(formatter, "formatter");
			Assert.ArgumentNotNull(rootFactory, "rootFactory");

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			_rootPath = InitializeRootPath(rootPath);

			_rootFactory = rootFactory;
			_formatter = formatter;
			_formatter.ParentDataStore = this;

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			Trees = InitializeTrees();
		}

		public void Save(IItemData item)
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
		public void MoveOrRenameItem(IItemData itemWithFinalPath, string oldPath)
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

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			var tree = GetTreeForPath(path, database);

			if (tree == null) return Enumerable.Empty<IItemData>();

			return tree.GetItemsByPath(path);
		}

		public IItemData GetByMetadata(IItemMetadata metadata, string database)
		{
			Assert.ArgumentNotNull(metadata, "metadata");
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.IsNotNullOrEmpty(metadata.Path, "The path is required to get an item from the SFS data store.");

			var items = GetByPath(metadata.Path, database);

			if (metadata.Id != default(Guid)) return items.FirstOrDefault(item => item.Id == metadata.Id);

			if (!items.Any()) return null;

			throw new AmbiguousMatchException("The path " + metadata.Path + " matched more than one item. Reduce ambiguity by passing the ID as well, or use GetByPath() for multiple results.");
		}

		public IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			var tree = GetTreeForPath(parentItem.Path, parentItem.DatabaseName);

			if (tree == null) throw new InvalidOperationException("No trees contained the global path " + parentItem.Path);

			return tree.GetChildren(parentItem);
		}

		public void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			// TODO: consistency check
			throw new NotImplementedException();
		}

		public void ResetTemplateEngine()
		{
			// do nothing, the YAML serializer has no template engine
		}

		public bool Remove(IItemData item)
		{
			var tree = GetTreeForPath(item.Path, item.DatabaseName);

			if (tree == null) return false;

			return tree.Remove(item);
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
			var trees = Trees.Where(tree => tree.DatabaseName.Equals(database) && tree.ContainsPath(path)).ToArray();

			if (trees.Length == 0)
			{
				return null;
			}

			if (trees.Length > 1) throw new InvalidOperationException("The trees {0} contained the global path {1} - overlapping trees are not allowed.".FormatWith(string.Join(", ", trees.Select(tree => tree.Name)), path));

			return trees[0];
		}

		protected virtual SerializationFileSystemTree CreateTree(TreeRoot root)
		{
			return new SerializationFileSystemTree(root.Name, root.Path, root.DatabaseName, Path.Combine(_rootPath, root.Name), _formatter);
		}
	}
}
