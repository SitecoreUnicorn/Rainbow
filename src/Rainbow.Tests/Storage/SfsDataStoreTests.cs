using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Rainbow.Tests.Storage
{
	public class SfsDataStoreTests
	{
		[Fact]
		public void Save_SavesItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore");

				dataStore.Save(item);
			}
		}

		[Fact]
		public void InitializeRootPath_RemovesDots()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), @"..\Items")))
			{
				Assert.False(dataStore.PhysicalRootPathAccessor.Contains(".."));
			}
		}

		[Fact]
		public void Save_ErrorWhenItemNotInTree()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/ektron");

				// ReSharper disable once AccessToDisposedClosure
				Assert.Throws<InvalidOperationException>(() => dataStore.Save(item));
			}
		}

		[Fact]
		public void MoveOrRename_RenamesItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var renamingItemGrandchild = new FakeItem(path: "/sitecore/test/hulk/smash", name: "smash", id: Guid.NewGuid());
				var renamingItemChild = new FakeItem(path: "/sitecore/test/hulk", name: "hulk", children: new[] { renamingItemGrandchild }, id: Guid.NewGuid());
				var itemToRename = new FakeItem(path: "/sitecore/test", name: "test", children: new[] { renamingItemChild }, id: Guid.NewGuid());
				var rootItem = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid(), children: new[] { itemToRename });

				dataStore.Save(rootItem);
				dataStore.Save(itemToRename);
				dataStore.Save(renamingItemChild);
				dataStore.Save(renamingItemGrandchild);

				// note adding children with old paths; method takes care of rewriting child paths
				var renamedItem = new FakeItem(id: itemToRename.Id, path: "/sitecore/hexed", name: "hexed", children: new[] { renamingItemChild });

				dataStore.MoveOrRenameItem(renamedItem, "/sitecore/test");

				var retrievedRenamedItem = dataStore.GetByPath("/sitecore/hexed", "master").ToArray();
				var retrievedRenamedChild = dataStore.GetByPath("/sitecore/hexed/hulk", "master").ToArray();
				var retrievedRenamedGrandchild = dataStore.GetByPath("/sitecore/hexed/hulk/smash", "master").ToArray();

				Assert.NotEmpty(retrievedRenamedItem);
				Assert.Equal("/sitecore/hexed", retrievedRenamedItem.First().Path);

				// verify children moved
				Assert.NotEmpty(retrievedRenamedChild);
				Assert.NotEmpty(retrievedRenamedGrandchild);
			}
		}

		[Fact]
		public void MoveOrRename_MovesItem_WhenDestinationIsParentOfSource()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var itemToMoveChild = new FakeItem(path: "/sitecore/test/hulk/smash", name: "smash", id: Guid.NewGuid());
				var itemToMove = new FakeItem(path: "/sitecore/test/hulk", name: "hulk", children: new[] { itemToMoveChild }, id: Guid.NewGuid());
				var parentItem = new FakeItem(path: "/sitecore/test", name: "test", children: new[] { itemToMove }, id: Guid.NewGuid());
				var destinationItem = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid(), children: new[] { parentItem });

				dataStore.Save(destinationItem);
				dataStore.Save(parentItem);
				dataStore.Save(itemToMove);
				dataStore.Save(itemToMoveChild);

				// note adding children with old paths; method takes care of rewriting child paths
				var renamedItem = new FakeItem(id: itemToMove.Id, path: "/sitecore/hexed", name: "hexed", children: new[] { itemToMoveChild });

				dataStore.MoveOrRenameItem(renamedItem, "/sitecore/test/hulk");

				var retrievedRenamedItem = dataStore.GetByPath("/sitecore/hexed", "master").ToArray();
				var retrievedRenamedChild = dataStore.GetByPath("/sitecore/hexed/smash", "master").ToArray();

				Assert.NotEmpty(retrievedRenamedItem);
				Assert.Equal("/sitecore/hexed", retrievedRenamedItem.First().Path);

				// verify children moved
				Assert.NotEmpty(retrievedRenamedChild);
				Assert.Equal("/sitecore/hexed/smash", retrievedRenamedChild.First().Path);
			}
		}

		[Fact]
		public void MoveOrRename_RenamesItem_WhenDestinationIsASubsetOfSourceName()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var startingItemName = "thumpy basscannon";
				var renameItemName = "thumpy";

				dataStore.CreateTestItemTree("/sitecore");

				var itemToRename = new FakeItem(path: $"/sitecore/{startingItemName}", name: startingItemName, id: Guid.NewGuid());

				dataStore.Save(itemToRename);

				// note adding children with old paths; method takes care of rewriting child paths
				var renamedItem = new FakeItem(id: itemToRename.Id, path: $"/sitecore/{renameItemName}", name: renameItemName);

				dataStore.MoveOrRenameItem(renamedItem, itemToRename.Path);

				var retrievedRenamedItem = dataStore.GetByPath($"/sitecore/{renameItemName}", "master").ToArray();

				retrievedRenamedItem.Length.Should().Be(1);
				retrievedRenamedItem.First().Path.Should().Be($"/sitecore/{renameItemName}");
				retrievedRenamedItem.First().SerializedItemId.Should().EndWith($"\\{renameItemName}.yml");
			}
		}

		[Fact]
		public void MoveOrRename_RenamesItem_WhenChildrenAreOnShortPaths()
		{
			// This test checks that moves and renames when children are on loopback paths succeed. See Unicorn#77 and Unicorn#81

			// this is the total test path length that is required to go to a loopback path, which is essential for this test (see SfsTree.cs, MaxRelativePathLength property)
			var testMaxRelativePath = 240;
			testMaxRelativePath -= 80; // 'expected max physical path length'
			testMaxRelativePath -= 2; // length of test tree name ('T0')
			testMaxRelativePath -= "/sitecore/".Length + 2; // length of root item name and separators

			// but the max path will be > 100 chars, so we have to split it up into two item segments so that we can hit the path max, starting with a 100-char base path
			var testBaseSegmentLength = 100;
			testMaxRelativePath -= testBaseSegmentLength;

			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var testBaseSegmentName = new string('a', testBaseSegmentLength);
				var testBaseSegmentPath = "/sitecore/" + testBaseSegmentName;

				// this will give us a path that when two characters are added goes into a loopback path
				var testRootName = new string('b', testMaxRelativePath - 2);
				var testRootPath = testBaseSegmentPath + "/" + testRootName;

				// this will give us an equivalent length path to rename into
				var renamedTestRootName = testRootName.Replace('b', 'c');
				var renamedTestRootPath = testBaseSegmentPath + "/" + renamedTestRootName;

				var renamingItemGrandchild = new FakeItem(path: testRootPath + "/hulk/smash", name: "smash", id: Guid.NewGuid());
				var renamingItemChild = new FakeItem(path: testRootPath + "/hulk", name: "hulk", children: new[] { renamingItemGrandchild }, id: Guid.NewGuid());
				var itemToRename = new FakeItem(path: testRootPath, name: testRootName, children: new[] { renamingItemChild }, id: Guid.NewGuid());
				var baseSegment = new FakeItem(path: testBaseSegmentPath, name: testBaseSegmentName, id: Guid.NewGuid(), children: new[] { itemToRename });
				var rootItem = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid(), children: new[] { baseSegment });

				dataStore.Save(rootItem);
				dataStore.Save(baseSegment);
				dataStore.Save(itemToRename);
				dataStore.Save(renamingItemChild);
				dataStore.Save(renamingItemGrandchild);

				// note adding children with old paths; method takes care of rewriting child paths
				var renamedItem = new FakeItem(id: itemToRename.Id, path: renamedTestRootPath, name: renamedTestRootName, children: new[] { renamingItemChild });

				dataStore.MoveOrRenameItem(renamedItem, testRootPath);

				var retrievedRenamedItem = dataStore.GetByPath(renamedTestRootPath, "master").ToArray();
				var retrievedRenamedChild = dataStore.GetByPath(renamedTestRootPath + "/hulk", "master").ToArray();
				var retrievedRenamedGrandchild = dataStore.GetByPath(renamedTestRootPath + "/hulk/smash", "master").ToArray();

				// Asserts
				retrievedRenamedItem.Should().NotBeEmpty("the renamed item should be available");
				retrievedRenamedItem.First().Path.Should().Be(renamedTestRootPath, "renamed item path should match expectation");

				// verify children moved
				Assert.NotEmpty(retrievedRenamedChild);
				Assert.NotEmpty(retrievedRenamedGrandchild);
			}
		}

		[Fact]
		public void GetByPath_RetrievesItemByPath()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore");

				dataStore.Save(item);

				var retrieved = dataStore.GetByPath("/sitecore", "master").ToArray();

				Assert.NotEmpty(retrieved);
				Assert.Equal("/sitecore", retrieved.First().Path);
			}
		}

		[Fact]
		public void GetByPathAndId_RetrievesItemByMetadataPath()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid());

				dataStore.Save(item);

				var retrieved = dataStore.GetByPathAndId(item.Path, item.Id, "master");

				Assert.NotNull(retrieved);
				Assert.Equal("/sitecore", retrieved.Path);
			}
		}

		[Fact]
		public void Remove_RemovesItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore");

				dataStore.Save(item);

				dataStore.Remove(item);

				var root = dataStore.GetByPath("/sitecore", "master");

				Assert.Empty(root);
			}
		}

		[Fact]
		public void GetChildren_GetsExpectedChildren()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var rootId = Guid.NewGuid();

				var item = new FakeItem(path: "/sitecore", name: "sitecore", id: rootId);

				dataStore.Save(item);

				var child = new FakeItem(path: "/sitecore/test", name: "test", parentId: rootId);

				dataStore.Save(child);

				var kids = dataStore.GetChildren(item).ToArray();

				Assert.NotEmpty(kids);
				Assert.Equal("/sitecore/test", kids.First().Path);
			}
		}

		[Fact]
		public void GetMetadataByTemplateId_GetsExpectedItem_WhenTargetIsAtRoot()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var templateId = Guid.NewGuid();

				var item = new FakeItem(path: "/sitecore", name: "sitecore", templateId: templateId);

				dataStore.Save(item);

				var byTemplate = dataStore.GetMetadataByTemplateId(templateId, "master").ToArray();

				Assert.Equal(1, byTemplate.Length);
				Assert.Equal(templateId, byTemplate[0].TemplateId);
			}
		}

		[Fact]
		public void GetMetadataByTemplateId_GetsExpectedItem_WhenTargetIsMultipleChildren()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var templateId = Guid.NewGuid();

				var item = new FakeItem(path: "/sitecore", name: "sitecore", templateId: Guid.NewGuid());
				var item2 = new FakeItem(path: "/sitecore/item1", name: "item1", templateId: templateId, id: Guid.NewGuid());
				var item3 = new FakeItem(path: "/sitecore/item1/item2", name: "item2", templateId: templateId, id: Guid.NewGuid());

				dataStore.Save(item);
				dataStore.Save(item2);
				dataStore.Save(item3);

				var byTemplate = dataStore.GetMetadataByTemplateId(templateId, "master").ToArray();

				Assert.Equal(2, byTemplate.Length);
				Assert.Equal(templateId, byTemplate[0].TemplateId);
			}
		}

		[Fact]
		public void Clear_ClearsTree()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore");

				dataStore.Save(item);

				dataStore.Clear();

				Assert.Empty(dataStore.GetByPath("/sitecore", "master"));
			}
		}

		[Fact]
		public void GetById_GetsExpectedItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var id = Guid.NewGuid();
				var item = new FakeItem(path: "/sitecore", id: id);

				dataStore.Save(item);

				Assert.NotNull(dataStore.GetById(id, "master"));
			}
		}

		[Fact]
		public void GetById_ReturnsNull_WhenDatabaseIsIncorrect()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var id = Guid.NewGuid();
				var item = new FakeItem(path: "/sitecore", id: id);

				dataStore.Save(item);

				Assert.Null(dataStore.GetById(id, "core"));
			}
		}

		[Fact]
		public void GetItem_ThrowsError_WhenOverlappingPaths()
		{
			Assert.Throws<InvalidOperationException>(() =>
			{
				using (var dataStore = new TestSfsDataStore(new[] { "/sitecore", "/sitecore/content" }))
				{
					dataStore.GetByPath("/sitecore/content/home", "master");
				}
			});
		}

		[Fact]
		public void GetItem_DoesNotThrowError_WhenSimilarNonOverlappingPaths()
		{
			using (var dataStore = new TestSfsDataStore(new[] { "/sitecore/content", "/sitecore/content cemetary" }))
			{
				dataStore.GetByPath("/sitecore/content cemetary/foo", "master");
			}
		}
	}
}
