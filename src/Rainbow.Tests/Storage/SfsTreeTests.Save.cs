using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Test]
		public void Save_WritesItem_WhenItemIsRoot_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				Assert.IsTrue(File.Exists(Path.Combine(testTree.PhysicalRootPathTest, "sitecore.yml")));
			}
		}

		[Test]
		public void Save_WritesItem_WhenItemIsNested_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/hello", testTree);

				Assert.IsTrue(File.Exists(Path.Combine(testTree.PhysicalRootPathTest, "sitecore", "hello.yml")));
			}
		}

		[Test]
		public void Save_WritesItem_WhenItemIsRoot_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				CreateTestTree("/sitecore/templates", testTree);

				Assert.IsTrue(File.Exists(Path.Combine(testTree.PhysicalRootPathTest, "templates.yml")));
			}
		}

		[Test]
		public void Save_WritesItem_WhenItemIsNested_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				CreateTestTree("/sitecore/templates/hello", testTree);

				Assert.IsTrue(File.Exists(Path.Combine(testTree.PhysicalRootPathTest, "templates", "hello.yml")));
			}
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 10 chars after the root path
				testTree.MaxPathLengthForTests = 10;
				CreateTestTree("/sitecore/content", testTree);

				var rootItem = testTree.GetRootItem();
				
				var loopedItem = testTree.GetChildren(rootItem).First();

				Assert.AreEqual("/sitecore/content", loopedItem.Path);
				// loopback path will have root item ID in it
				Assert.IsTrue(loopedItem.SerializedItemId.Contains(rootItem.Id.ToString()));
			}
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresChildOfLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = 50;

				// this tree is long enough to loopback, but the 'hello' is short enough to be a child of the first loopback at 'e'
				CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/e/hello", testTree);

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet").First();
                var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/e/hello").First();

				Assert.AreEqual("/sitecore/content lorem/ipsum dolor/sit amet/e/hello", helloItem.Path);
				// hello item will have looped id in it
				Assert.IsTrue(helloItem.SerializedItemId.Contains(loopParent.Id.ToString()));
			}
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresDoubleLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 10 chars after the root path
				// this also means that double loopback occurs each time because the loopback ID is 35 chars
				testTree.MaxPathLengthForTests = 10;

				CreateTestTree("/sitecore/content/hello", testTree);

				var rootItem = testTree.GetRootItem();

				var loopedItem = testTree.GetChildren(rootItem).First();

				var secondLoopedItem = testTree.GetChildren(loopedItem).First();

				Assert.AreEqual("/sitecore/content", loopedItem.Path);
				// loopback path will have root item ID in it
				Assert.IsTrue(loopedItem.SerializedItemId.Contains(rootItem.Id.ToString()));

				Assert.AreEqual("/sitecore/content/hello", secondLoopedItem.Path);
				// loopback path will have root item ID in it
				Assert.IsTrue(secondLoopedItem.SerializedItemId.Contains(loopedItem.Id.ToString()));
			}
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresChildOfDoubleLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = 50;

				// this tree is long enough that it will loopback at 'elitr foo bar baz', and that '{id}+/elitr foo bar baz' will make it loopback again on 'h', leaving the final 'hello' a child of the second loopback
				CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello", testTree);

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello").First();

				Assert.AreEqual("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello", helloItem.Path);
				// hello item will have looped id in it
				Assert.IsTrue(helloItem.SerializedItemId.Contains(loopParent.Id.ToString()));
			}
		}

		[Test]
		public void Save_WritesItem_WhenItemNameIsFullOfInvalidChars()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/%<html>?*", testTree);

				var rootItem = testTree.GetRootItem();

				var charsItem = testTree.GetChildren(rootItem).First();

				Assert.AreEqual("/sitecore/%<html>?*", charsItem.Path);
			}
		}

		[Test]
		public void Save_WritesItem_WhenItemNameIsTooLong()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				CreateTestTree("/sitecore/hello hello", testTree);

				var rootItem = testTree.GetRootItem();

				var overlengthItem = testTree.GetChildren(rootItem).First();

				Assert.AreEqual("/sitecore/hello hello", overlengthItem.Path);
				// name should be truncated
				Assert.IsTrue(overlengthItem.SerializedItemId.EndsWith("hello hell.yml"));
			}
		}

		[Test]
		public void Save_WritesExpectedItems_WhenItemNameIsTooLong_AndItemsWithSameShortenedNameExist()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				CreateTestTree("/sitecore/hello hello", testTree);

				testTree.Save(CreateTestItem("/sitecore/hello hello hello", testTree.GetRootItem().Id));

				var overlengthItems = testTree.GetChildren(testTree.GetRootItem()).ToArray();

				Assert.AreEqual(2, overlengthItems.Count());
				Assert.IsTrue(overlengthItems.Any(item => item.Path == "/sitecore/hello hello"));
				Assert.IsTrue(overlengthItems.Any(item => item.Path == "/sitecore/hello hello hello"));
			}
		}
	}
}
