using System;
using Rainbow.Model;

namespace Rainbow.Tests.Storage.SFS
{
	public partial class SfsTreeTests
	{
		private void CreateTestTree(string treePath, TestSfsTree tree)
		{
			var localPath = tree.ConvertGlobalPathToTreePathTest(treePath)
				.TrimStart('/')
				.Split('/');
			
			var contextPath = string.Empty;
			Guid currentParentId = default(Guid);

			foreach (var pathPiece in localPath)
			{
				// the first path piece will equal the last segment in the global root - don't double append
				if(pathPiece != localPath[0])
					contextPath += "/" + pathPiece;

				var item = CreateTestItem(tree.GlobalRootItemPath + contextPath, currentParentId);

				currentParentId = item.Id;

				tree.Save(item);
			}
		}

		private IItemData CreateTestItem(string path, Guid parentId)
		{
			var name = path.LastIndexOf('/') > -1 ? path.Substring(path.LastIndexOf('/') + 1) : path;

			return new FakeItem(databaseName: "UnitTesting", id: Guid.NewGuid(), name: name, path: path, parentId: parentId);
		}
	}
}
