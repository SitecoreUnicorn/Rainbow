using System.IO;
using System.Linq;
using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Fact]
		public void Save_WritesItem_WhenItemIsRoot_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.CreateTestTree("/sitecore");

				Assert.True(File.Exists(Path.Combine(testTree.PhysicalRootPath, "sitecore.yml")));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenItemIsNested_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.CreateTestTree("/sitecore/hello");

				Assert.True(File.Exists(Path.Combine(testTree.PhysicalRootPath, "sitecore", "hello.yml")));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenItemIsRoot_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				testTree.CreateTestTree("/sitecore/templates");

				Assert.True(File.Exists(Path.Combine(testTree.PhysicalRootPath, "templates.yml")));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenItemIsNested_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				testTree.CreateTestTree("/sitecore/templates/hello");

				Assert.True(File.Exists(Path.Combine(testTree.PhysicalRootPath, "templates", "hello.yml")));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenPathRequiresLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 10 chars after the root path
				testTree.MaxPathLengthForTests = 10;
				testTree.CreateTestTree("/sitecore/content");

				var rootItem = testTree.GetRootItem();

				var loopedItem = testTree.GetChildren(rootItem).First();

				Assert.Equal("/sitecore/content", loopedItem.Path);
				// loopback path will have root item ID in it
				Assert.True(loopedItem.SerializedItemId.Contains(rootItem.Id.ToString()));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenPathRequiresChildOfLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = 50;

				// this tree is long enough to loopback, but the 'hello' is short enough to be a child of the first loopback at 'e'
				testTree.CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/e/hello");

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/e/hello").First();

				Assert.Equal("/sitecore/content lorem/ipsum dolor/sit amet/e/hello", helloItem.Path);
				// hello item will have looped id in it
				Assert.True(helloItem.SerializedItemId.Contains(loopParent.Id.ToString()));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenPathRequiresDoubleLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 10 chars after the root path
				// this also means that double loopback occurs each time because the loopback ID is 35 chars
				testTree.MaxPathLengthForTests = 10;

				testTree.CreateTestTree("/sitecore/content/hello");

				var rootItem = testTree.GetRootItem();

				var loopedItem = testTree.GetChildren(rootItem).First();

				var secondLoopedItem = testTree.GetChildren(loopedItem).First();

				Assert.Equal("/sitecore/content", loopedItem.Path);
				// loopback path will have root item ID in it
				Assert.True(loopedItem.SerializedItemId.Contains(rootItem.Id.ToString()));

				Assert.Equal("/sitecore/content/hello", secondLoopedItem.Path);
				// loopback path will have root item ID in it
				Assert.True(secondLoopedItem.SerializedItemId.Contains(loopedItem.Id.ToString()));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenPathRequiresChildOfDoubleLoopbackFolder()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to loopback after only 50 chars after the root path
				testTree.MaxPathLengthForTests = 50;

				// this tree is long enough that it will loopback at 'elitr foo bar baz', and that '{id}+/elitr foo bar baz' will make it loopback again on 'h', leaving the final 'hello' a child of the second loopback
				testTree.CreateTestTree("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello");

				var loopParent = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz").First();
				var helloItem = testTree.GetItemsByPath("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello").First();

				Assert.Equal("/sitecore/content lorem/ipsum dolor/sit amet/elitr foo bar baz/h/hello", helloItem.Path);
				// hello item will have looped id in it
				Assert.True(helloItem.SerializedItemId.Contains(loopParent.Id.ToString()));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenItemNameIsFullOfInvalidChars()
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.CreateTestTree("/sitecore/%<html>?*");

				var rootItem = testTree.GetRootItem();

				var charsItem = testTree.GetChildren(rootItem).First();

				Assert.Equal("/sitecore/%<html>?*", charsItem.Path);
			}
		}

		[Fact]
		public void Save_WritesItem_WhenItemNameIsTooLong()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				testTree.CreateTestTree("/sitecore/hello hello");

				var rootItem = testTree.GetRootItem();

				var overlengthItem = testTree.GetChildren(rootItem).First();

				Assert.Equal("/sitecore/hello hello", overlengthItem.Path);
				// name should be truncated
				Assert.True(overlengthItem.SerializedItemId.EndsWith("hello hell.yml"));
			}
		}

		[Fact]
		public void Save_WritesItem_WhenRootPathIsRelative()
		{
			using (var testTree = new TestSfsTree("/../../Items", "/sitecore"))
			{
				testTree.CreateTestTree("/sitecore/hello");

				var rootItem = testTree.GetRootItem();

				var childItem = testTree.GetChildren(rootItem).First();

				Assert.Equal("/sitecore/hello", childItem.Path);
				Assert.EndsWith("\\Items\\sitecore\\hello.yml", childItem.SerializedItemId);
				Assert.False(childItem.SerializedItemId.Contains(".."));
			}
		}

		[Fact]
		public void Save_WritesExpectedItems_WhenItemNameIsTooLong_AndItemsWithSameShortenedNameExist()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				testTree.CreateTestTree("/sitecore/hello hello");

				testTree.Save("/sitecore/hello hello hello".AsTestItem(testTree.GetRootItem().Id));

				var overlengthItems = testTree.GetChildren(testTree.GetRootItem()).OrderBy(i => i.SerializedItemId).ToArray();

				Assert.Equal(2, overlengthItems.Length);
				Assert.Equal("/sitecore/hello hello", overlengthItems[0].Path);
				Assert.EndsWith("hello hell.yml", overlengthItems[0].SerializedItemId);
				Assert.Equal("/sitecore/hello hello hello", overlengthItems[1].Path);
				Assert.EndsWith("hello hell_" + overlengthItems[1].Id + ".yml", overlengthItems[1].SerializedItemId);
			}
		}

		[Fact]
		public void Save_WritesExpectedItems_WhenItemsWithSameNamePrefixExist()
		{
			using (var testTree = new TestSfsTree())
			{
				// longer name first
				testTree.CreateTestTree("/sitecore/Html Editor Drop Down Button");

				// shorter name second - name is unique, but has same prefix as longer
				testTree.Save("/sitecore/Html Editor Drop Down".AsTestItem(testTree.GetRootItem().Id));

				var children = testTree.GetChildren(testTree.GetRootItem()).OrderBy(i => i.SerializedItemId).ToArray();

				Assert.Equal(2, children.Length);
				Assert.Equal("/sitecore/Html Editor Drop Down Button", children[0].Path);
				Assert.EndsWith("Html Editor Drop Down Button.yml", children[0].SerializedItemId);
				Assert.Equal(children[1].Path, "/sitecore/Html Editor Drop Down");
				Assert.EndsWith("Html Editor Drop Down.yml", children[1].SerializedItemId);
			}
		}

		[Fact]
		public void Save_SetsSerializedItemId_WhenUsingDataCache()
		{
			using (var testTree = new TestSfsTree(useDataCache: true))
			{
				testTree.CreateTestTree("/sitecore");

				var item = testTree.GetItemsByPath("/sitecore").First();

				Assert.Equal(item.SerializedItemId, Path.Combine(testTree.PhysicalRootPath, "sitecore.yml"));
			}
		}
	}
}
