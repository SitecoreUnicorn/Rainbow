using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Rainbow.Storage;

namespace Rainbow.Tests.Storage
{
	public class SfsDataStoreTests
	{
		[Test]
		public void DataStore_SavesItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore");

				dataStore.Save(item);
			}
		}

		[Test]
		public void DataStore_Saves_ErrorWhenItemNotInTree()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/ektron");

				Assert.Throws<InvalidOperationException>(() => dataStore.Save(item));
			}
		}

		[Test]
		public void DataStore_MoveOrRename_RenamesItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid());

				dataStore.Save(item);

				var child = new FakeItem(path: "/sitecore/test", name: "test");

				dataStore.Save(child);

				var renamed = new FakeItem(path: "/sitecore/hexed", name: "hexed");

				dataStore.MoveOrRenameItem(renamed, "/sitecore/test");

				var retrieved = dataStore.GetByPath("/sitecore/hexed", "master").ToArray();

				Assert.IsNotEmpty(retrieved);
				Assert.AreEqual("/sitecore/hexed", retrieved.First().Path);
			}
		}

		[Test]
		public void DataStore_RetrievesItemByPath()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore");

				dataStore.Save(item);

				var retrieved = dataStore.GetByPath("/sitecore", "master").ToArray();

				Assert.IsNotEmpty(retrieved);
				Assert.AreEqual("/sitecore", retrieved.First().Path);
			}
		}

		[Test]
		public void DataStore_RetrievesItemByMetadataPath()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore", id: Guid.NewGuid());

				dataStore.Save(item);

				var retrieved = dataStore.GetByPathAndId(item.Path, item.Id, "master");

				Assert.IsNotNull(retrieved);
				Assert.AreEqual("/sitecore", retrieved.Path);
			}
		}

		[Test]
		public void DataStore_RemovesItem()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var item = new FakeItem(path: "/sitecore", name: "sitecore");

				dataStore.Save(item);

				dataStore.Remove(item);

				var root = dataStore.GetByPath("/sitecore", "master");

				Assert.IsEmpty(root);
			}
		}

		[Test]
		public void DataStore_GetChildren()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var rootId = Guid.NewGuid();

				var item = new FakeItem(path: "/sitecore", name: "sitecore", id: rootId);

				dataStore.Save(item);

				var child = new FakeItem(path: "/sitecore/test", name: "test", parentId: rootId);

				dataStore.Save(child);

				var kids = dataStore.GetChildren(item).ToArray();

				Assert.IsNotEmpty(kids);
				Assert.AreEqual("/sitecore/test", kids.First().Path);
			}
		}

		[Test]
		public void DataStore_GetMetadataByTemplateId_GetsExpectedItem_WhenTargetIsAtRoot()
		{
			using (var dataStore = new TestSfsDataStore("/sitecore"))
			{
				var templateId = Guid.NewGuid();

				var item = new FakeItem(path: "/sitecore", name: "sitecore", templateId: templateId);

				dataStore.Save(item);

				var byTemplate = dataStore.GetMetadataByTemplateId(templateId, "master").ToArray();

				Assert.AreEqual(1, byTemplate.Length);
				Assert.AreEqual(templateId, byTemplate[0].TemplateId);
			}
		}

		[Test]
		public void DataStore_GetMetadataByTemplateId_GetsExpectedItem_WhenTargetIsMultipleChildren()
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

				Assert.AreEqual(2, byTemplate.Length);
				Assert.AreEqual(templateId, byTemplate[0].TemplateId);
			}
		}
	}
}
