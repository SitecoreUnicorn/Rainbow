using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Rainbow.Formatting;
using Rainbow.Model;
using Rainbow.Storage.SFS;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Rainbow.Storage
{
	public class FileSystemDataStore : IDataStore
	{
		private readonly string _rootPath;
		private readonly ISerializationFormatter _formatter;
		protected readonly List<SfsTree> Trees;

		public FileSystemDataStore(string rootPath, ISerializationFormatter formatter)
		{
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");
			Assert.ArgumentNotNull(formatter, "formatter");

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			_rootPath = InitializeRootPath(rootPath);

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			Trees = InitializeTrees();

			_formatter = formatter;
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
				// TODO: this is unaware of any predicate restrictions - inclined to make that a Unicorn version injection
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

			if (tree == null) throw new InvalidOperationException("No trees contained the global path " + path);

			return tree.GetItemsByPath(path);
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

			if (tree == null) throw new InvalidOperationException("No trees contained the global path " + item.Path);

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

		protected virtual List<SfsTree> InitializeTrees()
		{
			return Directory.GetDirectories(_rootPath).Select(treeFolder => CreateTree(Path.GetFileName(treeFolder))).ToList();
		}

		protected virtual SfsTree GetTreeForPath(string path, string database)
		{
			// TODO: exclusionary processing would go here, yes? (but only in leaves style)

			var trees = Trees.Where(tree => tree.DatabaseName.Equals(database) && tree.ContainsPath(path)).ToArray();

			if (trees.Length == 0) return null;

			if (trees.Length > 1) throw new InvalidOperationException("The trees {0} contained the global path {1} - overlapping trees are not allowed.".FormatWith(string.Join(", ", trees.Select(tree => tree.Name)), path));

			return trees[0];
		}

		protected virtual SfsTree CreateTree(string databaseName)
		{
			return new SfsTree(databaseName, string.Empty, databaseName, Path.Combine(_rootPath, databaseName), _formatter);
		}
	}
}
