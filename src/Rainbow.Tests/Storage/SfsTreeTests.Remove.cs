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
				testTree.CreateTestTree("/sitecore");

				testTree.Remove(testTree.GetRootItem());

				Assert.Empty(Directory.GetFileSystemEntries(testTree.PhysicalRootPath));

				var root = testTree.GetRootItem();

				Assert.Null(root);
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemIsChild()
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.CreateTestTree("/sitecore/content/foo");

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
				testTree.CreateTestTree("/sitecore/content/foo/bar/baz/boing");

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
				testTree.CreateTestTree("/sitecore/content/foo/bar/baz/boing");

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
				testTree.MaxPathLengthForTests = testTree.PhysicalRootPath.Length + 50;

				// this tree is long enough to loopback, but the 'hello' is short enough to be a child of the first loopback at 'e'
				testTree.CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/e/hello");

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/e/hello").First();

				testTree.Remove(loopParent);

				Assert.False(File.Exists(loopParent.SerializedItemId));
				Assert.False(Directory.Exists(Path.Combine(testTree.PhysicalRootPath, loopParent.Id.ToString())));
				Assert.False(File.Exists(helloItem.SerializedItemId));
			}
		}

		[Fact]
		public void Remove_DeletesItem_WhenItemHasChildrenInDoubleLoopbackDirectory()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = testTree.PhysicalRootPath.Length + 50;

				// this tree is long enough that it will loopback at 'elitr foo bar baz', and that '{id}+/elitr foo bar baz' will make it loopback again on 'h', leaving the final 'hello' a child of the second loopback
				testTree.CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello");

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz").First();
				var loop2Parent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello").First();

				testTree.Remove(loopParent);

				Assert.False(File.Exists(loop2Parent.SerializedItemId));
				Assert.False(Directory.Exists(Path.Combine(testTree.PhysicalRootPath, loop2Parent.Id.ToString())));
				Assert.False(File.Exists(helloItem.SerializedItemId));
			}
		}
	}
}
