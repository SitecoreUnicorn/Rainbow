using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Rainbow.Storage.Sc.Deserialization;
using Rainbow.Tests;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.FakeDb;
using Xunit;

namespace Rainbow.Storage.Sc.Tests
{
	public class SitecoreDataStoreTests
	{
		[Fact]
		public void GetDatabaseNames_ShouldReturnDatabaseNames()
		{

		}

		[Fact]
		public void Save_ShouldDeserializeItem()
		{
			var deserializer = Substitute.For<IDeserializer>();
			var dataStore = new SitecoreDataStore(deserializer);

			var item = new FakeItem();

			dataStore.Save(item, null);

			deserializer.Received().Deserialize(item, null);
		}

		[Fact]
		public void MoveOrRename_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.MoveOrRenameItem(new FakeItem(), "/sitecore"));
		}

		[Theory, AutoDbData]
		public void GetByPath_ReturnsExpectedItem_WhenPathExists(Db db)
		{
			var dataStore = CreateTestDataStore();

			var item = dataStore.GetByPath("/sitecore/templates", "master").ToArray();

			Assert.NotEmpty(item);
			Assert.Equal("templates", item.First().Name);
		}

		[Theory, AutoDbData]
		public void GetByPath_ReturnsNull_WhenPathDoesNotExist(Db db)
		{
			var dataStore = CreateTestDataStore();

			dataStore.GetByPath("/sitecore/templates/Monkey Bars", "master").Should().BeEmpty();
		}

		[Fact]
		public void GetByPathAndId_ReturnsItem_WhenItemIdExists()
		{
			//GetById_ReturnsItem_WhenItemIdExists(); // for now. in case implementation changes later, keep this redundant test.
		}

		[Theory, AutoDbData]
		public void GetById_ReturnsItem_WhenItemIdExists(Db db)
		{
			var id = new ID();
			db.Add(new DbItem("Hello", id));

			var dataStore = CreateTestDataStore();

			var item = dataStore.GetById(id.Guid, "master");

			Assert.NotNull(item);
			Assert.Equal("Hello", item.Name);
		}

		[Theory, AutoDbData]
		public void GetById_ReturnsNull_WhenItemIdDoesNotExist(Db db)
		{
			var dataStore = CreateTestDataStore();

			dataStore.GetById(Guid.NewGuid(), "master").Should().BeNull();
		}

		[Fact]
		public void GetMetadataByTemplateId_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.GetMetadataByTemplateId(Guid.NewGuid(), "master"));
		}

		[Theory, AutoDbData]
		public void GetChildren_ReturnsExpectedChildren(Db db)
		{
			db.Add(new DbItem("Hello"));

			var dataStore = CreateTestDataStore();

			var parent = dataStore.GetByPath("/sitecore/content", "master").First();

			var children = dataStore.GetChildren(parent);

			Assert.NotNull(children);
			Assert.NotEmpty(children);
			Assert.Equal("Hello", children.First().Name);
		}

		[Theory, AutoDbData]
		public void GetChildren_ReturnsEmptyEnumerable_IfItemDoesNotExist(Db db)
		{
			var dataStore = CreateTestDataStore();

			var parent = new FakeItem(Guid.NewGuid(), name: "LOL");

			dataStore.GetChildren(parent).Should().NotBeNull().And.BeEmpty();
		}

		[Theory, AutoDbData]
		public void Remove_RemovesExpectedItem([Content]Item item)
		{
			var dataStore = CreateTestDataStore();

			var dsItem = dataStore.GetById(item.ID.Guid, "master");

			var result = dataStore.Remove(dsItem);

			var itemAfterDelete = dataStore.GetById(item.ID.Guid, "master");

			result.Should().BeTrue();
			itemAfterDelete.Should().BeNull();
		}

		[Theory, AutoDbData]
		public void Remove_ReturnsFalse_WhenItemDoesNotExist(Db db)
		{
			var dataStore = CreateTestDataStore();

			var item = new FakeItem(Guid.NewGuid(), name: "lol");

			dataStore.Remove(item).Should().BeFalse();
		}

		[Fact]
		public void RegisterForChanges_ThrowsNotImplementedException()
		{
			var dataStore = CreateTestDataStore();
			Assert.Throws<NotImplementedException>(() => dataStore.RegisterForChanges((metadata, s) => { }));
		}

		[Fact]
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
