using System.Linq;
using NUnit.Framework;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Test]
		public void GetChildren_ReturnsExpectedItem_WhenRootPathIsParent_AndTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore/templates", testTree);

				var root = testTree.GetRootItem();

				var children = testTree.GetChildren(root).ToArray();

				Assert.IsNotNull(children);
				Assert.AreEqual(1, children.Length);
				Assert.AreEqual(children[0].Name, "templates");
			}
		}

		[Test]
		public void GetChildren_ReturnsExpectedItem_WhenRootPathIsParent_AndTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				CreateTestTree("/sitecore/templates/User Defined", testTree);

				var root = testTree.GetItemsByPath("/sitecore/templates").First();

				var children = testTree.GetChildren(root).ToArray();

				Assert.IsNotNull(children);
				Assert.AreEqual(1, children.Length);
				Assert.AreEqual(children[0].Name, "User Defined");
			}
		}

		[Test]
		public void GetChildren_ReturnsExpectedItem_WhenRootPathIsParent_AndTreeIsNested_AndCacheIsCleared()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates"))
			{
				CreateTestTree("/sitecore/templates/User Defined", testTree);

				var root = testTree.GetItemsByPath("/sitecore/templates").First();

				testTree.ClearAllCaches();

				var children = testTree.GetChildren(root).ToArray();

				Assert.IsNotNull(children);
				Assert.AreEqual(1, children.Length);
				Assert.AreEqual(children[0].Name, "User Defined");
			}
		}

		[Test]
		public void GetChildren_ReturnsExpectedItems_WhenMultipleMatchesExist()
		{
			using (var testTree = new TestSfsTree())
			{
				const string treePath = "/sitecore/templates";
				CreateTestTree(treePath, testTree);

				var testItem = testTree.GetItemsByPath(treePath);

				// add a second child item
				testTree.Save(CreateTestItem("/sitecore/system", testItem.First().ParentId));

				// get the children of the root, which should include the two items
				var results = testTree.GetChildren(testTree.GetRootItem()).ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreNotEqual(results[0].Id, results[1].Id);
				Assert.AreNotEqual(results[0].SerializedItemId, results[1].SerializedItemId);
				Assert.IsTrue(results.Any(result => result.Name == "templates"));
				Assert.IsTrue(results.Any(result => result.Name == "system"));
			}
		}

		[Test]
		public void GetChildren_ReturnsExpectedItems_WhenMultipleSameNamedMatchesExist()
		{
			using (var testTree = new TestSfsTree())
			{
				const string treePath = "/sitecore/templates";
				CreateTestTree(treePath, testTree);

				var testItem = testTree.GetItemsByPath(treePath);

				// add a second templates item
				testTree.Save(CreateTestItem(treePath, testItem.First().ParentId));

				// get the children of the root, which should include the two same named items
				var results = testTree.GetChildren(testTree.GetRootItem()).ToArray();

				Assert.AreEqual(2, results.Length);
				Assert.AreNotEqual(results[0].Id, results[1].Id);
				Assert.AreNotEqual(results[0].SerializedItemId, results[1].SerializedItemId);
			}
		}

		[Test]
		public void GetChildren_ReturnsExpectedItems_WhenMultipleMatchesExist_ThroughSeparateParents()
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

				// get the children of templates1, which should NOT include templates2's child
				var results = testTree.GetChildren(templates1).ToArray();

				Assert.AreEqual(1, results.Length);
				Assert.AreEqual("User Defined", results[0].Name);
			}
		}

		[Test]
		public void GetChildren_ReturnsEmptyEnumerable_WhenNoChildrenExist()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				// get the children of the root, which be empty
				var results = testTree.GetChildren(testTree.GetRootItem());

				Assert.IsNotNull(results);
				Assert.IsEmpty(results);
			}
		}

		[Test]
		public void GetChildren_ReturnsExpectedItem_WhenNamesContainInvalidPathChars()
		{
			using (var testTree = new TestSfsTree("/<html>"))
			{
				CreateTestTree("/<html>/$head", testTree);

				var root = testTree.GetRootItem();

				var children = testTree.GetChildren(root).ToArray();

				Assert.IsNotNull(children);
				Assert.AreEqual(1, children.Length);
				Assert.AreEqual(children[0].Name, "$head");
			}
		}
	}
}
