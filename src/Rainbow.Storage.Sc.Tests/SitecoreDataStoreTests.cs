using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rainbow.Storage.Sc.Deserialization;
using Rainbow.Tests;
using Sitecore.Data;
using Sitecore.FakeDb;

namespace Rainbow.Storage.Sc.Tests
{
	public class SitecoreDataStoreTests
	{
		[Test]
		public void GetDatabaseNames_ShouldReturnDatabaseNames()
		{

		}

		[Test]
		public void Save_ShouldDeserializeItem()
		{
			var deserializer = Substitute.For<IDeserializer>();
			var dataStore = new SitecoreDataStore(deserializer);

			var item = new FakeItem();

			dataStore.Save(item);

			deserializer.Received().Deserialize(item, Arg.Any<bool>());
		}

		[Test]
		public void MoveOrRename_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.MoveOrRenameItem(new FakeItem(), "/sitecore"));
		}

		[Test]
		public void GetByPath_ReturnsExpectedItem_WhenPathExists()
		{
			using (new Db())
			{
				var dataStore = CreateTestDataStore();

				var item = dataStore.GetByPath("/sitecore/templates", "master").ToArray();

				Assert.IsNotEmpty(item);
				Assert.AreEqual("templates", item.First().Name);
			}
		}

		[Test]
		public void GetByPath_ReturnsNull_WhenPathDoesNotExist()
		{
			using (new Db())
			{
				var dataStore = CreateTestDataStore();

				var item = dataStore.GetByPath("/sitecore/templates/Monkey Bars", "master");

				Assert.IsEmpty(item);
			}
		}

		[Test]
		public void GetByPathAndId_ReturnsItem_WhenItemIdExists()
		{
			GetById_ReturnsItem_WhenItemIdExists(); // for now. in case implementation changes later, keep this redundant test.
		}

		[Test]
		public void GetById_ReturnsItem_WhenItemIdExists()
		{
			using (var db = new Db())
			{
				var id = new ID();
				db.Add(new DbItem("Hello", id));

				var dataStore = CreateTestDataStore();

				var item = dataStore.GetById(id.Guid, "master");

				Assert.IsNotNull(item);
				Assert.AreEqual("Hello", item.Name);
			}
		}

		[Test]
		public void GetById_ReturnsNull_WhenItemIdDoesNotExist()
		{
			using (new Db())
			{
				var dataStore = CreateTestDataStore();

				var item = dataStore.GetById(Guid.NewGuid(), "master");

				Assert.IsNull(item);
			}
		}

		[Test]
		public void GetMetadataByTemplateId_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.GetMetadataByTemplateId(Guid.NewGuid(), "master"));
		}

		[Test]
		public void GetChildren_ReturnsExpectedChildren()
		{
			using (var db = new Db())
			{
				db.Add(new DbItem("Hello"));

				var dataStore = CreateTestDataStore();

				var parent = dataStore.GetByPath("/sitecore/content", "master").First();

				var children = dataStore.GetChildren(parent);

				Assert.IsNotNull(children);
				Assert.IsNotEmpty(children);
				Assert.AreEqual("Hello", children.First().Name);
			}
		}

		[Test]
		public void GetChildren_ReturnsEmptyEnumerable_IfItemDoesNotExist()
		{
			using (new Db())
			{
				var dataStore = CreateTestDataStore();

				var parent = new FakeItem(Guid.NewGuid(), name: "LOL");

				var children = dataStore.GetChildren(parent);

				Assert.IsNotNull(children);
				Assert.IsEmpty(children);
			}
		}

		[Test]
		public void Remove_RemovesExpectedItem()
		{
			using (var db = new Db())
			{
				var id = new ID();
				db.Add(new DbItem("Hello", id));

				var dataStore = CreateTestDataStore();

				var item = dataStore.GetById(id.Guid, "master");

				var result = dataStore.Remove(item);

				var itemAfterDelete = dataStore.GetById(id.Guid, "master");

				Assert.IsTrue(result);
				Assert.IsNull(itemAfterDelete);
			}
		}

		[Test]
		public void Remove_ReturnsFalse_WhenItemDoesNotExist()
		{
			using (new Db())
			{
				var dataStore = CreateTestDataStore();

				var item = new FakeItem(Guid.NewGuid(), name: "lol");

				var result = dataStore.Remove(item);

				Assert.IsFalse(result);
			}
		}

		[Test]
		public void RegisterForChanges_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.RegisterForChanges((metadata, s) => { }));
		}

		[Test]
		public void Clear_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.Clear());
		}

		protected IDataStore CreateTestDataStore()
		{
			var deserializer = Substitute.For<IDeserializer>();

			return new SitecoreDataStore(deserializer);
		}
	}
}
