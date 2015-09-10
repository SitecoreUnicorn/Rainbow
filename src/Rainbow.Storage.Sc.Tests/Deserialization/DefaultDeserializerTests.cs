using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rainbow.Filtering;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Rainbow.Tests;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.FakeDb;

namespace Rainbow.Storage.Sc.Tests.Deserialization
{
	public class DefaultDeserializerTests
	{
		private readonly ID _testTemplateId = ID.NewID;
		private readonly ID _testTemplate2Id = ID.NewID;
		private readonly ID _testSharedFieldId = ID.NewID;
		private readonly ID _testVersionedFieldId = ID.NewID;

		[Test]
		public void Deserialize_DeserializesNewItem()
		{
			using (var db = new Db())
			{
				var deserializer = CreateTestDeserializer(db);

				var item = new FakeItem(
					id: Guid.NewGuid(),
					parentId: ItemIDs.ContentRoot.Guid,
					templateId: _testTemplateId.Guid,
					versions: new[]
					{
						new FakeItemVersion(1, "en", new[]
						{
							new FakeFieldValue("Hello", fieldId: _testVersionedFieldId.Guid)
						})
					});

				var deserialized = deserializer.Deserialize(item, false);

				Assert.IsNotNull(deserialized);

				var fromDb = db.GetItem(new ID(item.Id));

				Assert.IsNotNull(fromDb);
				Assert.AreEqual("Hello", fromDb[_testVersionedFieldId]);
				Assert.AreEqual(item.ParentId, fromDb.ParentID.Guid);
				Assert.AreEqual(item.TemplateId, fromDb.TemplateID.Guid);
			}
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithSharedFieldChanges()
		{
			RunItemChangeTest(
				setup: itemData =>
				 {
					 var fields = itemData.SharedFields.ToList();
					 fields.Add(new FakeFieldValue("Shared Value", fieldId: _testSharedFieldId.Guid));
					 itemData.SharedFields = fields;
				 },
				assert: dbItem =>
				{
					Assert.AreEqual("Shared Value", dbItem[_testSharedFieldId]);
				}
			);
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithVersionedFieldChanges()
		{
			RunItemChangeTest(
				setup: itemData =>
				{
					var version = (ProxyItemVersion)itemData.Versions.First();
					var fields = version.Fields.ToList();
					fields.Add(new FakeFieldValue("Versioned Value", fieldId: _testVersionedFieldId.Guid));
					version.Fields = fields;
				},
				assert: dbItem =>
				{
					Assert.AreEqual("Versioned Value", dbItem[_testVersionedFieldId]);
				}
			);
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithRenamed()
		{
			RunItemChangeTest(
				setup: itemData =>
				{
					itemData.Name = "Testy Item";
				},
				assert: dbItem =>
				{
					Assert.AreEqual("Testy Item", dbItem.Name);
				}
			);
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithMoved()
		{
			RunItemChangeTest(
				setup: itemData =>
				{
					itemData.ParentId = ItemIDs.TemplateRoot.Guid;
				},
				assert: dbItem =>
				{
					Assert.AreEqual(ItemIDs.TemplateRoot, dbItem.ParentID);
					Assert.AreEqual("/sitecore/templates/test item", dbItem.Paths.FullPath);
				}
			);
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithTemplateChanged()
		{
			RunItemChangeTest(
				setup: itemData =>
				{
					itemData.TemplateId = _testTemplate2Id.Guid;
				},
				assert: dbItem =>
				{
					Assert.AreEqual(_testTemplate2Id, dbItem.TemplateID);
				}
			);
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithVersionAdded()
		{
			RunItemChangeTest(
				setup: itemData =>
				{
					var versions = itemData.Versions.ToList();
					versions.Add(new FakeItemVersion(2));

					itemData.Versions = versions;
				},
				assert: dbItem =>
				{
					Assert.AreEqual(2, dbItem.Versions.Count);
				}
			);
		}

		[Test]
		public void Deserialize_DeserializesExistingItem_WithVersionRemoved()
		{
			RunItemChangeTest(
				setup: itemData =>
				{
					var versions = new List<IItemVersion>();
					itemData.Versions = versions;
				},
				assert: dbItem =>
				{
					Assert.AreEqual(0, dbItem.Versions.Count);
				}
			);
		}

		[Test]
		public void Deserialize_IgnoresField_ExcludedWithFieldFilter()
		{
			var ignoredFieldId = ID.NewID;

			var fieldFilter = Substitute.For<IFieldFilter>();
			fieldFilter.Includes(Arg.Any<Guid>()).Returns(true);
			fieldFilter.Includes(ignoredFieldId.Guid).Returns(false);

			var deserializer = new DefaultDeserializer(Substitute.For<IDefaultDeserializerLogger>(), fieldFilter);
			deserializer.ParentDataStore = new SitecoreDataStore(deserializer);

			using (var db = new Db())
			{
				var itemId = ID.NewID;

				db.Add(new DbItem("Test Item", itemId)
				{
					{ignoredFieldId, "Test Value"}
				});

				var itemData = new ProxyItem(new ItemData(db.GetItem(itemId)));

				var fields = new List<IItemFieldValue>();
				fields.Add(new FakeFieldValue("Changed Ignored Value", fieldId:ignoredFieldId.Guid));
				((ProxyItemVersion)itemData.Versions.First()).Fields = fields;

				deserializer.Deserialize(itemData, false);

				var fromDb = db.GetItem(itemId);

				Assert.AreEqual(fromDb[ignoredFieldId], "Test Value");
			}
		}

		protected IDeserializer CreateTestDeserializer(Db db)
		{
			var fieldFilter = Substitute.For<IFieldFilter>();
			fieldFilter.Includes(Arg.Any<Guid>()).Returns(true);

			var deserializer = new DefaultDeserializer(Substitute.For<IDefaultDeserializerLogger>(), fieldFilter);
			deserializer.ParentDataStore = new SitecoreDataStore(deserializer);

			db.Add(new DbTemplate("Test Template", _testTemplateId)
			{
				new DbField("Test Field", _testVersionedFieldId),
				new DbField("Test Shared", _testSharedFieldId) { Shared = true }
			});

			db.Add(new DbTemplate("Test Template2", _testTemplate2Id)
			{
				new DbField("Test Field"),
				new DbField("Test Shared") { Shared = true }
			});

			return deserializer;
		}

		protected ID AddExistingItem(Db db, Action<DbItem> customize = null)
		{
			ID id = ID.NewID;
			var item = new DbItem("test item", id, _testTemplateId);
			if (customize != null) customize(item);

			db.Add(item);

			return id;
		}

		private void RunItemChangeTest(Action<ProxyItem> setup, Action<Item> assert)
		{
			using (var db = new Db())
			{
				var deserializer = CreateTestDeserializer(db);

				var itemId = AddExistingItem(db);

				var itemData = new ProxyItem(new ItemData(db.GetItem(itemId)));

				setup(itemData);

				deserializer.Deserialize(itemData, false);

				var fromDb = db.GetItem(itemId);

				Assert.IsNotNull(fromDb);
				assert(fromDb);
			}
		}
	}
}
