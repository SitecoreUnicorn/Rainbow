using System;
using System.Linq;
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
				var greatgrandchild = new FakeItem(path: "/sitecore/test/hulk/smash", name: "smash", id: Guid.NewGuid());
				var grandchild = new FakeItem(path: "/sitecore/test/hulk", name: "hulk", children: new[] { greatgrandchild }, id: Guid.NewGuid());
				var child = new FakeItem(path: "/sitecore/test", name: "test", children: new[] { grandchild }, id: Guid.NewGuid());
				var item = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid(), children: new[] { child });

				dataStore.Save(item);
				dataStore.Save(child);
				dataStore.Save(grandchild);
				dataStore.Save(greatgrandchild);

				// note adding children with old paths; method takes care of rewriting child paths
				var renamed = new FakeItem(path: "/sitecore/hexed", name: "hexed", children: new[] { grandchild });

				dataStore.MoveOrRenameItem(renamed, "/sitecore/test");

				var retrieved = dataStore.GetByPath("/sitecore/hexed", "master").ToArray();
				var retrievedGrandchild = dataStore.GetByPath("/sitecore/hexed/hulk", "master").ToArray();
				var retrievedGreatGrandchild = dataStore.GetByPath("/sitecore/hexed/hulk/smash", "master").ToArray();

				Assert.NotEmpty(retrieved);
				Assert.Equal("/sitecore/hexed", retrieved.First().Path);

				// verify children moved
				Assert.NotEmpty(retrievedGrandchild);
				Assert.NotEmpty(retrievedGreatGrandchild);
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
	}
}
