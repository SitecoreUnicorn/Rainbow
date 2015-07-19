using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Rainbow.Tests.Storage.SFS
{
	partial class SfsTreeTests
	{
		[Test]
		public void Remove_DeletesItem_WhenItemIsRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				testTree.Remove(testTree.GetRootItem());

				Assert.IsEmpty(Directory.GetFileSystemEntries(testTree.PhysicalRootPathTest));

				var root = testTree.GetRootItem();

				Assert.IsNull(root);
			}
		}

		[Test]
		public void Remove_DeletesItem_WhenItemIsChild()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content/foo").First();

				testTree.Remove(item);

				Assert.IsEmpty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.IsEmpty(testTree.GetItemsByPath("/sitecore/content/foo"));
			}
		}

		[Test]
		public void Remove_DeletesItem_WhenItemHasChildren()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo/bar/baz/boing", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content").First();

				testTree.Remove(item);

				Assert.IsEmpty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.IsEmpty(testTree.GetItemsByPath("/sitecore/content"));
			}
		}

		[Test]
		public void Remove_DeletesItem_WhenItemHasChildrenInLoopbackDirectory()
		{
			throw new NotImplementedException();
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo/bar/baz/boing", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content").First();

				testTree.Remove(item);

				Assert.IsEmpty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.IsEmpty(testTree.GetItemsByPath("/sitecore/content"));
			}
		}

		[Test]
		public void Remove_DeletesItem_WhenItemHasChildrenInDoubleLoopbackDirectory()
		{
			throw new NotImplementedException();
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/content/foo/bar/baz/boing", testTree);

				var item = testTree.GetItemsByPath("/sitecore/content").First();

				testTree.Remove(item);

				Assert.IsEmpty(Directory.GetFileSystemEntries(Path.GetDirectoryName(item.SerializedItemId)));
				Assert.IsEmpty(testTree.GetItemsByPath("/sitecore/content"));
			}
		}
	}
}
