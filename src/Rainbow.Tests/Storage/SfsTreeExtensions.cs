using System;
using Rainbow.Storage;

namespace Rainbow.Tests.Storage
{
	static class SfsTreeExtensions
	{
		public static void CreateTestTree(this SerializationFileSystemTree tree, string globalPath, string database = "master")
		{
			var localPath = tree.ConvertGlobalVirtualPathToTreeVirtualPath(globalPath)
				.TrimStart('/')
				.Split('/');

			var contextPath = string.Empty;
			Guid currentParentId = default(Guid);
			FakeItem currentParent = null;

			foreach (var pathPiece in localPath)
			{
				// the first path piece will equal the last segment in the global root - don't double append
				if (pathPiece != localPath[0])
					contextPath += "/" + pathPiece;

				var item = AsTestItem(tree.GlobalRootItemPath + contextPath, currentParentId, databaseName: database);

				currentParentId = item.Id;

				currentParent?.SetProxyChildren(new[] { item });
				currentParent = item;

				tree.Save(item);
			}
		}

		public static FakeItem AsTestItem(this string path, Guid parentId, Guid templateId = default(Guid), string databaseName = "UnitTesting")
		{
			var name = path.LastIndexOf('/') > -1 ? path.Substring(path.LastIndexOf('/') + 1) : path;

			return new FakeItem(databaseName: databaseName, id: Guid.NewGuid(), name: name, path: path, parentId: parentId, templateId: templateId);
		}
	}
}
