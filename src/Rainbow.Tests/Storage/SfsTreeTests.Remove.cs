using System.IO;
using System.Linq;
using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Fact]
		public void Remove_DeletesItem_WhenItemIsRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				testTree.Remove(testTree.GetRootItem());

				Assert.Empty(Directory.GetFileSystemEntries(testTree.PhysicalRootPathTest));

				var root = testTree.GetRootItem();

				Assert.Null(root);
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemIsChild()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content/foo").First();

				testTree.Remove(item);

				Assert.Empty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.Empty(testTree.GetItemsByPath("/sitecore/content/foo"));
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemHasChildren()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo/bar/baz/boing", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content").First();

				testTree.Remove(item);

				Assert.Empty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.Empty(testTree.GetItemsByPath("/sitecore/content"));
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemHasChildren_AndCacheIsEmpty()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo/bar/baz/boing", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content").First();

				testTree.ClearAllCaches();

				testTree.Remove(item);

				Assert.Empty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.Empty(testTree.GetItemsByPath("/sitecore/content"));
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemHasChildrenInLoopbackDirectory()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = testTree.PhysicalRootPathTest.Length + 50;

				// this tree is long enough to loopback, but the 'hello' is short enough to be a child of the first loopback at 'e'
				CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/e/hello", testTree);

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/e/hello").First();

				testTree.Remove(loopParent);

				Assert.False(File.Exists(loopParent.SerializedItemId));
				Assert.False(Directory.Exists(Path.Combine(testTree.PhysicalRootPathTest, loopParent.Id.ToString())));
				Assert.False(File.Exists(helloItem.SerializedItemId));
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemHasChildrenInDoubleLoopbackDirectory()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = testTree.PhysicalRootPathTest.Length + 50;

				// this tree is long enough that it will loopback at 'elitr foo bar baz', and that '{id}+/elitr foo bar baz' will make it loopback again on 'h', leaving the final 'hello' a child of the second loopback
				CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello", testTree);

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz").First();
				var loop2Parent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello").First();

				testTree.Remove(loopParent);

				Assert.False(File.Exists(loop2Parent.SerializedItemId));
				Assert.False(Directory.Exists(Path.Combine(testTree.PhysicalRootPathTest, loop2Parent.Id.ToString())));
				Assert.False(File.Exists(helloItem.SerializedItemId));
			}
		}
	}
}
