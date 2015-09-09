using System;
using System.Linq;
using NUnit.Framework;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.FakeDb;

namespace Rainbow.Storage.Sc.Tests
{
	public class ItemDataTests
	{
		[Test]
		public void Id_ReturnsExpectedValue()
		{
			var id = ID.NewID;
			var item = new DbItem("hello", id);

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual(id.Guid, testItem.Id);
			});
		}

		[Test]
		public void DatabaseName_ReturnsExpectedValue()
		{
			var item = new DbItem("hello");

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual("master", testItem.DatabaseName);
			});
		}

		[Test]
		public void ParentId_ReturnsExpectedValue()
		{
			var pid = ID.NewID;
			var item = new DbItem("hello", pid);

			RunItemTest(item, (testItem, db) =>
			{
				db.Add(new DbItem("hellochild") { ParentID = pid });

				var child = db.GetItem("/sitecore/content/hello/hellochild");

				Assert.AreEqual(pid, child.ParentID);
			});
		}

		[Test]
		public void Path_ReturnsExpectedValue()
		{
			var item = new DbItem("hello");

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual("/sitecore/content/hello", testItem.Path);
			});
		}

		[Test]
		public void Name_ReturnsExpectedValue()
		{
			var item = new DbItem("hello");

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual("hello", testItem.Name);
			});
		}

		[Test]
		public void BranchId_ReturnsExpectedValue()
		{
			var id = ID.NewID;
			var item = new DbItem("hello") { BranchId = id };

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual(id.Guid, testItem.BranchId);
			});
		}

		[Test]
		public void TemplateId_ReturnsExpectedValue()
		{
			var id = ID.NewID;
			var item = new DbItem("hello") { TemplateID = id };

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual(id.Guid, testItem.TemplateId);
			});
		}

		[Test]
		public void SharedFields_ReturnsExpectedValues()
		{
			var id = ID.NewID;
			var item = new DbItem("hello")
			{
				new DbField(id) { Shared = true, Value = "test field"}
			};

			RunItemTest(item, (testItem, db) =>
			{
				Assert.IsTrue(testItem.SharedFields.Any(f => f.FieldId == id.Guid));
			});
		}

		[Test]
		public void Versions_ReturnsExpectedVersions()
		{
			var id = ID.NewID;
			var item = new DbItem("hello")
			{
				new DbField(id)
				{
					{"en", 1, "test value"},
					{"en", 2, "test v2"}
				}
			};

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual(2, testItem.Versions.Count());
				Assert.IsTrue(testItem.Versions.Any(v => v.Language.Name == "en" && v.VersionNumber == 1));
				Assert.IsTrue(testItem.Versions.Any(v => v.Language.Name == "en" && v.VersionNumber == 2));
			});
		}

		[Test]
		public void Field_Value_ReturnsExpectedValue()
		{
			var id = ID.NewID;
			var item = new DbItem("hello")
			{
				new DbField(id)
				{
					Shared = true,
					Value = "test"
				}
			};

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual(id.Guid, testItem.SharedFields.First(f => f.FieldId == id.Guid).FieldId);
			});
		}

		[Test]
		public void Field_FieldType_ReturnsExpectedValue()
		{
			var id = ID.NewID;
			var item = new DbItem("hello")
			{
				new DbField(id) {Value = "test", Type = "test type", Shared = true}
			};

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual("test type", testItem.SharedFields.First(f => f.FieldId == id.Guid).FieldType);
			});
		}

		[Test]
		public void Field_NameHint_ReturnsExpectedValue()
		{
			var id = ID.NewID;
			var item = new DbItem("hello")
			{
				new DbField(id) {Value = "test", Name = "Foo", Shared = true}
			};

			RunItemTest(item, (testItem, db) =>
			{
				Assert.AreEqual("Foo", testItem.SharedFields.First(f => f.FieldId == id.Guid).NameHint);
			});
		}

		[Test]
		public void Version_Fields_ReturnsExpectedValues()
		{
			var id = ID.NewID;
			var item = new DbItem("hello")
			{
				new DbField(id)
				{
					{"en", 1, "test"}
				}
			};

			RunItemTest(item, (testItem, db) =>
			{
				var version = testItem.Versions.First();
				Assert.AreEqual("test", version.Fields.FirstOrDefault(f => f.FieldId == id.Guid).Value);
			});
		}

		private void RunItemTest(DbItem testItem, Action<IItemData, Db> test)
		{
			using (var db = new Db())
			{
				db.Add(testItem);

				var item = db.GetItem(testItem.ID);

				test(new ItemData(item), db);
			}
		}
	}
}
