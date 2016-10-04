using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using FluentAssertions;
using Xunit;
using Rainbow.Diff;
using Rainbow.Diff.Fields;
using Rainbow.Model;

namespace Rainbow.Tests.Diff
{
	public class ItemComparerTests
	{
		[Fact]
		public void ItemComparer_IsNotEqual_WhenVersionedFieldsAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Hello")) });
			var targetItem = new FakeItem(versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Goodbye")) });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.Equal(1, comparison.ChangedVersions.Length);
			Assert.Equal(1, comparison.ChangedVersions[0].ChangedFields.Length);
			Assert.Equal("Hello", comparison.ChangedVersions[0].ChangedFields[0].SourceField.Value);
			Assert.Equal("Goodbye", comparison.ChangedVersions[0].ChangedFields[0].TargetField.Value);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenSharedFieldsAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Hello") });
			var targetItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Goodbye") });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.Equal(1, comparison.ChangedSharedFields.Length);
			Assert.Equal("Hello", comparison.ChangedSharedFields[0].SourceField.Value);
			Assert.Equal("Goodbye", comparison.ChangedSharedFields[0].TargetField.Value);
		}

		[Fact]
		public void FastCompare_IsNotEqual_WhenSharedFieldsAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Hello") });
			var targetItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Goodbye") });

			var comparison = comparer.FastCompare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeFalse();
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenSharedFieldIsInSourceOnly()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Hello") });
			var targetItem = new FakeItem();

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeFalse();
			comparison.ChangedSharedFields.Length.Should().Be(1);
			comparison.ChangedSharedFields[0].SourceField.Value.Should().Be("Hello");
			comparison.ChangedSharedFields[0].TargetField.Should().BeNull();
		}

		[Fact]
		public void ItemComparer_IsEqual_WhenSharedFieldIsInSourceOnly_AndValueIsEmpty()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(sharedFields: new[] { new FakeFieldValue(string.Empty) });
			var targetItem = new FakeItem();

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeTrue();
			comparison.ChangedSharedFields.Length.Should().Be(0);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenSharedFieldIsInTargetOnly()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem();
			var targetItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Hello") });

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeFalse();
			comparison.ChangedSharedFields.Length.Should().Be(1);
			comparison.ChangedSharedFields[0].TargetField.Value.Should().Be("Hello");
			comparison.ChangedSharedFields[0].SourceField.Should().BeNull();
		}

		[Fact]
		public void ItemComparer_IsEqual_WhenSharedFieldIsInTargetOnly_AndValueIsEmpty()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem();
			var targetItem = new FakeItem(sharedFields: new[] { new FakeFieldValue(string.Empty) });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.True(comparison.AreEqual);
			Assert.Equal(0, comparison.ChangedSharedFields.Length);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenUnversionedFieldsAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(unversionedFields: new[] { new ProxyItemLanguage(new CultureInfo("en")) { Fields = new[] { new FakeFieldValue("Hello") } } });
			var targetItem = new FakeItem(unversionedFields: new[] { new ProxyItemLanguage(new CultureInfo("en")) { Fields = new[] { new FakeFieldValue("Goodbye") } } });

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeFalse();
			comparison.ChangedUnversionedFields.Length.Should().Be(1);
			comparison.ChangedUnversionedFields[0].Language.Language.Should().Be(new CultureInfo("en"));
			comparison.ChangedUnversionedFields[0].ChangedFields.Count.Should().Be(1);
			comparison.ChangedUnversionedFields[0].ChangedFields[0].SourceField.Value.Should().Be("Hello");
			comparison.ChangedUnversionedFields[0].ChangedFields[0].TargetField.Value.Should().Be("Goodbye");
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenUnversionedFieldIsInSourceOnly()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(unversionedFields: new[] { new ProxyItemLanguage(new CultureInfo("en")) { Fields = new[] { new FakeFieldValue("Hello") } } });
			var targetItem = new FakeItem();

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeFalse();
			comparison.ChangedUnversionedFields.Length.Should().Be(1);
			comparison.ChangedUnversionedFields[0].ChangedFields.Count.Should().Be(1);
			comparison.ChangedUnversionedFields[0].ChangedFields[0].SourceField.Value.Should().Be("Hello");
			comparison.ChangedUnversionedFields[0].ChangedFields[0].TargetField.Should().BeNull();
		}

		[Fact]
		public void ItemComparer_IsEqual_WhenUnversionedFieldIsInSourceOnly_AndValueIsEmpty()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(unversionedFields: new[] { new ProxyItemLanguage(new CultureInfo("en")) { Fields = new[] { new FakeFieldValue(string.Empty) } } });
			var targetItem = new FakeItem();

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeTrue();
			comparison.ChangedUnversionedFields.Length.Should().Be(0);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenUnversionedFieldIsInTargetOnly()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem();
			var targetItem = new FakeItem(unversionedFields: new[] { new ProxyItemLanguage(new CultureInfo("en")) { Fields = new[] { new FakeFieldValue("Hello") } } });

			var comparison = comparer.Compare(sourceItem, targetItem);

			comparison.AreEqual.Should().BeFalse();
			comparison.ChangedUnversionedFields.Length.Should().Be(1);
			comparison.ChangedUnversionedFields[0].ChangedFields.Count.Should().Be(1);
			comparison.ChangedUnversionedFields[0].ChangedFields[0].TargetField.Value.Should().Be("Hello");
			comparison.ChangedUnversionedFields[0].ChangedFields[0].SourceField.Should().BeNull();
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenNewTargetVersionExists()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(versions: new[] { new FakeItemVersion() });
			var targetItem = new FakeItem(versions: new[] { new FakeItemVersion(), new FakeItemVersion(versionNumber: 2) });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.Equal(1, comparison.ChangedVersions.Length);
			Assert.Null(comparison.ChangedVersions[0].SourceVersion);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenNewSourceVersionExists()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(versions: new[] { new FakeItemVersion(), new FakeItemVersion(versionNumber: 2) });
			var targetItem = new FakeItem(versions: new[] { new FakeItemVersion() });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.Equal(1, comparison.ChangedVersions.Length);
			Assert.Null(comparison.ChangedVersions[0].TargetVersion);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenMoved()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(parentId: Guid.NewGuid());
			var targetItem = new FakeItem(parentId: Guid.NewGuid());

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.True(comparison.IsMoved);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenNamesAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(name: "Bork Bork Bork");
			var targetItem = new FakeItem(name: "Swedish Chef");

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.True(comparison.IsRenamed);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenTemplatesAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(templateId: Guid.NewGuid());
			var targetItem = new FakeItem(templateId: Guid.NewGuid());

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.True(comparison.IsTemplateChanged);
		}

		[Fact]
		public void ItemComparer_IsNotEqual_WhenBranchesAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(branchId: Guid.NewGuid());
			var targetItem = new FakeItem(branchId: Guid.NewGuid());

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.False(comparison.AreEqual);
			Assert.True(comparison.IsBranchChanged);
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_WhenItemsAreEqual()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(
				versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Hello")) },
				sharedFields: new[] { new FakeFieldValue("Goodbye") });
			var targetItem = new FakeItem(
				versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Hello")) },
				sharedFields: new[] { new FakeFieldValue("Goodbye") });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.True(comparison.AreEqual);
			Assert.Empty(comparison.ChangedSharedFields);
			Assert.Empty(comparison.ChangedVersions);
			Assert.False(comparison.IsMoved || comparison.IsRenamed || comparison.IsTemplateChanged);
		}

		// Commented out due to a bug in Sitecore 8.2 RTM that causes Factory.CreateObject<T>() to throw a null ref exception in a test context
		// FTW!
		//[Fact]
		//public void ItemComparer_AddsComparerFromXmlConfig()
		//{
		//	var xmlConfigNode = @"<itemComparer>
		//			<fieldComparer type=""Rainbow.Diff.Fields.XmlComparison, Rainbow"" />
		//		</itemComparer>";

		//	var configDoc = new XmlDocument();
		//	configDoc.LoadXml(xmlConfigNode);

		//	var comparer = new TestComparisonItemComparer(configDoc.DocumentElement);

		//	Assert.True(comparer.Comparers.Any(c => c.GetType() == typeof(XmlComparison)));
		//}

		private class TestItemComparer : ItemComparer
		{
			public TestItemComparer() : base(new List<IFieldComparer> { new DefaultComparison() })
			{

			}
		}

		private class TestComparisonItemComparer : ItemComparer
		{
			public TestComparisonItemComparer(XmlNode configNode) : base(configNode)
			{
			}

			public IFieldComparer[] Comparers => FieldComparers.ToArray();
		}
	}
}
