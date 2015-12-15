using System;
using System.Linq;
using FluentAssertions;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.FakeDb;
using Xunit;

namespace Rainbow.Storage.Sc.Tests
{
	public class ItemDataTests
	{

		[Theory, AutoDbData]
		public void Id_ReturnsExpectedValue([Content]Item item)
		{
			new ItemData(item).Id.Should().Be(item.ID.Guid);
		}

		[Theory, AutoDbData]
		public void DatabaseName_ReturnsExpectedValue([Content]Item item)
		{
			new ItemData(item).DatabaseName.Should().Be(item.Database.Name);
		}

		[Theory, AutoDbData]
		public void ParentId_ReturnsExpectedValue([Content]Item item)
		{
			new ItemData(item).ParentId.Should().Be(item.ParentID.Guid);
		}

		[Theory, AutoDbData]
		public void Path_ReturnsExpectedValue([Content]Item item)
		{
			new ItemData(item).Path.Should().Be(item.Paths.Path);
		}

		[Theory, AutoDbData]
		public void Name_ReturnsExpectedValue([Content]Item item)
		{
			new ItemData(item).Name.Should().Be(item.Name);
		}

		[Theory, AutoDbData]
		public void BranchId_ReturnsExpectedValue(Db db, DbItem item, Guid branchId)
		{
			item.BranchId = new ID(branchId);
			db.Add(item);

			var dbItem = db.GetItem(item.ID);

			new ItemData(dbItem).BranchId.Should().Be(branchId);
		}

		[Theory, AutoDbData]
		public void TemplateId_ReturnsExpectedValue([Content]Item item)
		{
			new ItemData(item).TemplateId.Should().Be(item.TemplateID.Guid);
		}

		[Theory, AutoDbData]
		public void SharedFields_ReturnsExpectedValues(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId)) { Shared = true, Value = "test field" });

			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).SharedFields.Any(f => f.FieldId == fieldId).Should().BeTrue();
		}

		[Theory, AutoDbData]
		public void SharedFields_DoesNotReturnVersionedValues(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId)) { Value = "test field" });

			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).SharedFields.Any(f => f.FieldId == fieldId).Should().BeFalse();
		}

		[Theory, AutoDbData]
		public void Versions_ReturnsExpectedVersions(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId))
				{
					{"en", 1, "test value"},
					{"en", 2, "test v2"}
				});

			var dbItem = db.CreateItem(item);
			var itemData = new ItemData(dbItem);

			dbItem.Versions.Count.Should().Be(itemData.Versions.Count());
			itemData.Versions.Any(v => v.Language.Name == "en" && v.VersionNumber == 1).Should().BeTrue();
			itemData.Versions.Any(v => v.Language.Name == "en" && v.VersionNumber == 2).Should().BeTrue();
		}

		[Theory, AutoDbData]
		public void Field_Value_ReturnsExpectedValue(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId))
			{
				Shared = true,
				Value = "test"
			});
		
			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).SharedFields.First(f => f.FieldId == fieldId).FieldId.Should().Be(fieldId);
		}

		[Theory, AutoDbData]
		public void Field_FieldType_ReturnsExpectedValue(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId))
			{
				Type = "test type",
				Shared = true,
				Value = "foo"
			});

			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).SharedFields.First(f => f.FieldId == fieldId).FieldType.Should().Be("test type");
		}

		[Theory, AutoDbData]
		public void Field_NameHint_ReturnsExpectedValue(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId))
			{
				Name = "Foo",
				Shared = true,
				Value = "foo"
			});

			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).SharedFields.First(f => f.FieldId == fieldId).NameHint.Should().Be("Foo");
		}

		[Theory, AutoDbData]
		public void Version_Fields_ReturnsExpectedValues(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId))
			{
				{"en", 1, "test"}
			});

			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).Versions.First().Fields.First(f => f.FieldId == fieldId).Value.Should().Be("test");
		}

		[Theory, AutoDbData]
		public void Version_Fields_DoesNotReturnSharedValues(Db db, DbItem item, Guid fieldId)
		{
			item.Fields.Add(new DbField(new ID(fieldId)) {Shared = true, Value = "test"});

			var dbItem = db.CreateItem(item);

			new ItemData(dbItem).Versions.First().Fields.FirstOrDefault(f => f.FieldId == fieldId).Should().BeNull();
		}
	}
}
