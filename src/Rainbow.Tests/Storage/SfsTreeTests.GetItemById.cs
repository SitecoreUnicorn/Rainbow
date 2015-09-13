using System.IO;
using System.Linq;
using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Fact]
		public void GetItemById_ResolvesItem_WhenItemIsRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				var root = testTree.GetRootItem();

				var byId = testTree.GetItemById(root.Id);

				Assert.NotNull(byId);
				Assert.Equal(root.Id, byId.Id);
			}
		}

		[Fact]
		public void GetItemById_ResolvesItem_WhenItemIsChild()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content/foo").First();

				var byId = testTree.GetItemById(item.Id);

				Assert.NotNull(byId);
				Assert.Equal(item.Id, byId.Id);
			}
		}

		[Fact]
		public void GetItemById_ResolvesItem_WhenItemIsRoot_AndCacheIsEmpty()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				var root = testTree.GetRootItem();

				testTree.ClearAllCaches();

				var byId = testTree.GetItemById(root.Id);

				Assert.NotNull(byId);
				Assert.Equal(root.Id, byId.Id);
			}
		}

		[Fact]
		public void GetItemById_ResolvesItem_WhenItemIsChild_AndCacheIsEmpty()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content/foo").First();

				testTree.ClearAllCaches();

				var byId = testTree.GetItemById(item.Id);

				Assert.NotNull(byId);
				Assert.Equal(item.Id, byId.Id);
			}
		}
	}
}
