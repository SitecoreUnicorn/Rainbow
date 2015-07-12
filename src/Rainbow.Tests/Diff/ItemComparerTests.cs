using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rainbow.Diff;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff
{
	public class ItemComparerTests
	{
		[Test]
		public void ItemComparer_IsNotEqual_WhenVersionedFieldsAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Hello")) });
			var targetItem = new FakeItem(versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Goodbye")) });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.AreEqual(1, comparison.ChangedVersions.Length);
			Assert.AreEqual(1, comparison.ChangedVersions[0].ChangedFields.Length);
			Assert.AreEqual("Hello", comparison.ChangedVersions[0].ChangedFields[0].SourceField.Value);
			Assert.AreEqual("Goodbye", comparison.ChangedVersions[0].ChangedFields[0].TargetField.Value);
		}

		[Test]
		public void ItemComparer_IsNotEqual_WhenSharedFieldsAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Hello") });
			var targetItem = new FakeItem(sharedFields: new[] { new FakeFieldValue("Goodbye") });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.AreEqual(1, comparison.ChangedSharedFields.Length);
			Assert.AreEqual("Hello", comparison.ChangedSharedFields[0].SourceField.Value);
			Assert.AreEqual("Goodbye", comparison.ChangedSharedFields[0].TargetField.Value);
		}

		[Test]
		public void ItemComparer_IsNotEqual_WhenNewTargetVersionExists()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(versions: new[] { new FakeItemVersion() });
			var targetItem = new FakeItem(versions: new[] { new FakeItemVersion(), new FakeItemVersion(versionNumber: 2) });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.AreEqual(1, comparison.ChangedVersions.Length);
			Assert.IsNull(comparison.ChangedVersions[0].SourceVersion);
		}

		[Test]
		public void ItemComparer_IsNotEqual_WhenNewSourceVersionExists()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(versions: new[] { new FakeItemVersion(), new FakeItemVersion(versionNumber: 2) });
			var targetItem = new FakeItem(versions: new[] { new FakeItemVersion() });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.AreEqual(1, comparison.ChangedVersions.Length);
			Assert.IsNull(comparison.ChangedVersions[0].TargetVersion);
		}

		[Test]
		public void ItemComparer_IsNotEqual_WhenMoved()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(parentId:Guid.NewGuid());
			var targetItem = new FakeItem(parentId: Guid.NewGuid());

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.IsTrue(comparison.IsMoved);
		}

		[Test]
		public void ItemComparer_IsNotEqual_WhenNamesAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(name: "Bork Bork Bork");
			var targetItem = new FakeItem(name: "Swedish Chef");

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.IsTrue(comparison.IsRenamed);
		}

		[Test]
		public void ItemComparer_IsNotEqual_WhenTemplatesAreUnequal()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(templateId:Guid.NewGuid());
			var targetItem = new FakeItem(templateId: Guid.NewGuid());

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsFalse(comparison.AreEqual);
			Assert.IsTrue(comparison.IsTemplateChanged);
		}

		[Test]
		public void EvaluateUpdate_DoesNotDeserialize_WhenItemsAreEqual()
		{
			var comparer = new TestItemComparer();

			var sourceItem = new FakeItem(
				versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Hello")) }, 
				sharedFields: new[] {new FakeFieldValue("Goodbye") });
			var targetItem = new FakeItem(
				versions: new[] { new FakeItemVersion(fields: new FakeFieldValue("Hello")) },
				sharedFields: new[] { new FakeFieldValue("Goodbye") });

			var comparison = comparer.Compare(sourceItem, targetItem);

			Assert.IsTrue(comparison.AreEqual);
			Assert.IsEmpty(comparison.ChangedSharedFields);
			Assert.IsEmpty(comparison.ChangedVersions);
			Assert.IsFalse(comparison.IsMoved || comparison.IsRenamed || comparison.IsTemplateChanged);
		}

		private class TestItemComparer : ItemComparer
		{
			public TestItemComparer() : base(new List<IFieldComparer> { new DefaultComparison() })
			{

			}
		}
	}
}
