using System.Linq;
using NUnit.Framework;

namespace Rainbow.Tests.Storage.SFS
{
	partial class SfsTreeTests
	{
		[Test]
		public void GetItemsByPath_ReturnsExpectedItem_WhenRootPathIsRequested_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				var root = testTree.GetItemsByPath("/sitecore").ToArray();

				Assert.IsNotNull(root);
				Assert.IsNotEmpty(root);
				Assert.AreEqual(root.First().Name, "sitecore");
			}
		}

		[Test]
		public void GetItemsByPath_ReturnsExpectedItem_WhenRootPathIsRequested_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				CreateTestTree("/sitecore/templates", testTree);

				var root = testTree.GetItemsByPath("/sitecore/templates").ToArray();

				Assert.IsNotNull(root);
				Assert.IsNotEmpty(root);
				Assert.AreEqual(root.First().Name, "templates");
			}
		}

		[Test]
		public void GetItemsByPath_ReturnsExpectedItem_WhenChildPathIsRequested_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/templates/User Defined", testTree);

				var root = testTree.GetItemsByPath("/sitecore/templates/User Defined").ToArray();

				Assert.IsNotNull(root);
				Assert.IsNotEmpty(root);
				Assert.AreEqual(root.First().Name, "User Defined");
			}
		}

		[Test]
		public void GetItemsByPath_ReturnsExpectedItem_WhenChildPathIsRequested_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				CreateTestTree("/sitecore/templates/User Defined", testTree);

				var root = testTree.GetItemsByPath("/sitecore/templates/User Defined").ToArray();

				Assert.IsNotNull(root);
				Assert.IsNotEmpty(root);
				Assert.AreEqual(root.First().Name, "User Defined");
			}
		}

		[Test]
		public void GetItemsByPath_ReturnsExpectedItem_WhenChildPathIsRequested_AndNamesContainInvalidPathChars()
		{
			using (var testTree = new TestSfsTree("/?hello*"))
			{
				CreateTestTree("/?hello*/%there%", testTree);

				var root = testTree.GetItemsByPath("/?hello*/%there%").ToArray();

				Assert.IsNotNull(root);
				Assert.IsNotEmpty(root);
				Assert.AreEqual(root.First().Name, "%there%");
			}
		}

		[Test]
		public void GetItemsByPath_ReturnsExpectedItems_WhenChildPathIsRequested_AndMultipleMatchesExist()
		{
			using (var testTree = new TestSfsTree())
			{
				const string treePath = "/sitecore/templates/User Defined";
				CreateTestTree(treePath, testTree);

				var testItem = testTree.GetItemsByPath(treePath);

				// add a second User Defined item
				testTree.Save(CreateTestItem(treePath, testItem.First().ParentId));

				var results = testTree.GetItemsByPath(treePath).ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreNotEqual(results[0].Id, results[1].Id);
				Assert.AreNotEqual(results[0].SerializedItemId, results[1].SerializedItemId);
			}
		}

		[Test]
		public void GetItemsByPath_ReturnsExpectedItems_WhenChildPathIsRequested_AndMultipleMatchesExist_ThroughSeparateParents()
		{
			using (var testTree = new TestSfsTree())
			{
				const string treePath = "/sitecore/templates/User Defined";

				CreateTestTree(treePath, testTree);

				var testItem = testTree.GetItemsByPath("/sitecore/templates");

				var templates1 = testItem.First();

				// add a second Templates item
				var templates2 = CreateTestItem("/sitecore/templates", templates1.ParentId);
				testTree.Save(templates2);

				// add a child under the second templates item, giving us '/sitecore/templates/User Defined' under templates1, and '/sitecore/templates/Evil' under templates2
				// P.S. don't actually do this in real life. Please? But I'm testing it, because I'm an effing pedant :)
				testTree.Save(CreateTestItem("/sitecore/templates/Evil", templates2.Id));

				var results = testTree.GetItemsByPath("/sitecore/templates").ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreNotEqual(results[0].Id, results[1].Id);
				Assert.AreNotEqual(results[0].SerializedItemId, results[1].SerializedItemId);
				Assert.IsTrue(results.Any(result => result.Id == templates1.Id));
				Assert.IsTrue(results.Any(result => result.Id == templates2.Id));
			}
		}

		[Test]
		public void GetItemByPath_GetsExpectedItem_WhenItemNameIsTooLong()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				CreateTestTree("/sitecore/hello hello", testTree);

				var overlengthItem = testTree.GetItemsByPath("/sitecore/hello hello").ToArray();

				Assert.AreEqual(1, overlengthItem.Count());

				Assert.AreEqual("/sitecore/hello hello", overlengthItem.First().Path);
			}
		}

		[Test]
		public void GetItemByPath_GetsExpectedItem_WhenPathIsAChildOfShortenedName()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				CreateTestTree("/sitecore/hello hello/goodbye", testTree);

				var overlengthChild = testTree.GetItemsByPath("/sitecore/hello hello/goodbye").ToArray();

				Assert.AreEqual(1, overlengthChild.Count());

				Assert.AreEqual("/sitecore/hello hello/goodbye", overlengthChild.First().Path);
			}
		}

		[Test]
		public void GetItemByPath_GetsExpectedItem_WhenItemNameIsTooLong_AndItemsWithSameShortenedNameExist()
		{
			using (var testTree = new TestSfsTree())
			{
				// force the tree to shorten after 10 char names
				testTree.MaxFileNameLengthForTests = 10;
				CreateTestTree("/sitecore/hello hello", testTree);

				testTree.Save(CreateTestItem("/sitecore/hello hello hello", testTree.GetRootItem().Id));

				var overlengthItem = testTree.GetItemsByPath("/sitecore/hello hello").ToArray();

				Assert.AreEqual(1, overlengthItem.Count());
				Assert.AreEqual("/sitecore/hello hello", overlengthItem.First().Path);
			}
		}
	}
}
