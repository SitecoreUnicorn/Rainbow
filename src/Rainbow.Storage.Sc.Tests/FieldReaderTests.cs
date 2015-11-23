using System.Linq;
using FluentAssertions;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.FakeDb;
using Xunit;

namespace Rainbow.Storage.Sc.Tests
{
	public class FieldReaderTests
	{
		[Theory,
			// Value tests (value should be returned)
			InlineData("Hello", null, "Hello"),

			// Standard values tests (do not return standard values)
			InlineData(null, "Hello", null),

			// Value overriding standard value tests (return value)
			InlineData("No", "Hello", "No"),

			// Blank values should not be returned
			InlineData("", null, null),

			// But blank values with a different standard value should be returned
			InlineData("", "1", ""),

			// And blank values with a blank standard value should not be returned
			InlineData("", "", null)
			]
		public void ParseFields_ReturnsExpectedValues(string fieldValue, string standardValue, string expected)
		{
			using (var db = new Db())
			{
				var testFieldId = ID.NewID;

				var template = new DbTemplate("Test Template") { { testFieldId, standardValue } };

				db.Add(template);

				var testItem = ItemManager.CreateItem("Test", db.GetItem(ItemIDs.ContentRoot), template.ID);

				if (expected != null)
				{
					using (new EditContext(testItem))
					{
						testItem[testFieldId] = expected;
					}
				}

				var sut = new FieldReader();

				testItem.Fields.ReadAll();

				var result = sut.ParseFields(testItem, false).FirstOrDefault(f => f.FieldId == testFieldId.Guid);

				
				if (expected == null) result.Should().BeNull();
				else result.Value.Should().Be(expected);
			}
		}
	}
}
