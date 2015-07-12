using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Rainbow.Diff.Fields;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Rainbow.Diff
{
	/// <summary>
	/// Compares two items and gets the results.
	/// NOTE: all item fields are compared. If you want to filter what you compare,
	/// use IItemFilter and pass in FilteredItem() instances to remove fields you don't want.
	/// </summary>
	public class ItemComparer : IItemComparer
	{
		protected readonly List<IFieldComparer> FieldComparers = new List<IFieldComparer>();

		public ItemComparer(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			var comparers = configNode.ChildNodes;

			foreach (XmlNode comparer in comparers)
			{
				if (comparer.NodeType == XmlNodeType.Element && comparer.Name.Equals("fieldComparer"))
					FieldComparers.Add(Factory.CreateObject<IFieldComparer>(comparer));
			}

			FieldComparers.Add(new DefaultComparison());
		}

		protected ItemComparer(List<IFieldComparer> fieldComparers)
		{
			Assert.ArgumentNotNull(fieldComparers, "fieldComparers");

			FieldComparers = fieldComparers;
		}

		/// <summary>
		/// Compares two items and returns detailed differences between the two
		/// </summary>
		public virtual ItemComparisonResult Compare(IItemData sourceItem, IItemData targetItem)
		{
			return CompareInternal(sourceItem, targetItem, false);
		}

		/// <summary>
		/// Compares two items and determines if they are equal or not, without details of the differences.
		/// </summary>
		/// <returns>True if equal, false if not equal</returns>
		public bool SimpleCompare(IItemData sourceItem, IItemData targetItem)
		{
			return CompareInternal(sourceItem, targetItem, true).AreEqual;
		}

		protected virtual ItemComparisonResult CompareInternal(IItemData sourceItem, IItemData targetItem, bool abortOnChangeFound)
		{
			Assert.ArgumentNotNull(targetItem, "targetItem");
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			bool moved = sourceItem.ParentId != targetItem.ParentId;

			// check if templates are different
			bool templateChanged = IsTemplateChanged(sourceItem, targetItem);

			if(abortOnChangeFound && templateChanged) return new ItemComparisonResult(sourceItem, targetItem, isTemplateChanged: true);

			// check if names are different
			bool renamed = IsRenamed(sourceItem, targetItem);

			if (abortOnChangeFound && renamed) return new ItemComparisonResult(sourceItem, targetItem, isRenamed: true);

			var changedVersions = new List<ItemVersionComparisonResult>();

			// check if source has version(s) that serialized does not
			var orphanVersions = sourceItem.Versions
				.Where(sourceVersion => GetVersion(targetItem, sourceVersion.Language.Name, sourceVersion.VersionNumber) == null)
				.ToArray();

			if (orphanVersions.Length > 0)
			{
				// source contained versions not present in the serialized version, which is a difference
				changedVersions.AddRange(orphanVersions.Select(version => new ItemVersionComparisonResult(version, null, null)));

				if(abortOnChangeFound) return new ItemComparisonResult(sourceItem, targetItem, changedVersions: changedVersions.ToArray());
			}

			// check if shared fields have any mismatching values
			var changedSharedFields = GetFieldDifferences(sourceItem.SharedFields, targetItem.SharedFields, sourceItem, targetItem, abortOnChangeFound);

			if(changedSharedFields.Length > 0 && abortOnChangeFound) return new ItemComparisonResult(sourceItem, targetItem, changedSharedFields:changedSharedFields);

			// see if the serialized versions have any mismatching values in the source data
			var changedTargetVersions = new List<ItemVersionComparisonResult>();
			foreach (var targetVersion in targetItem.Versions)
			{
				var sourceVersion = GetVersion(sourceItem, targetVersion.Language.Name, targetVersion.VersionNumber);

				// version exists in target item but does not in source item
				if (sourceVersion == null)
				{
					changedTargetVersions.Add(new ItemVersionComparisonResult(null, targetVersion, null));
					if (abortOnChangeFound) break;
					continue;
				}

				// field values mismatch
				var changedFields = GetFieldDifferences(sourceVersion.Fields, targetVersion.Fields, sourceItem, targetItem, abortOnChangeFound);
				if (changedFields.Length > 0)
				{
					// field changes found
					changedTargetVersions.Add(new ItemVersionComparisonResult(sourceVersion, targetVersion, changedFields));
					if (abortOnChangeFound) break;
				}
			}

			changedVersions.AddRange(changedTargetVersions);

			return new ItemComparisonResult(sourceItem, targetItem, renamed, moved, templateChanged, changedSharedFields, changedVersions.ToArray());
		}

		protected virtual bool IsRenamed(IItemData existingItemData, IItemData serializedItemData)
		{
			return !serializedItemData.Name.Equals(existingItemData.Name);
		}

		protected virtual bool IsTemplateChanged(IItemData existingItemData, IItemData serializedItemData)
		{
			if (existingItemData.TemplateId == default(Guid) && serializedItemData.TemplateId == default(Guid)) return false;

			return !serializedItemData.TemplateId.Equals(existingItemData.TemplateId);
		}

		protected virtual FieldComparisonResult[] GetFieldDifferences(IEnumerable<IItemFieldValue> sourceFields, IEnumerable<IItemFieldValue> targetFields, IItemData existingItemData, IItemData serializedItemData, bool abortOnChangeFound)
		{
			var targetFieldIndex = targetFields.ToDictionary(x => x.FieldId);

			var changedFields = new List<FieldComparisonResult>();

			foreach (var sourceField in sourceFields)
			{
				bool isDifferent = IsFieldDifferent(sourceField, targetFieldIndex, sourceField.FieldId);

				if (isDifferent)
				{
					changedFields.Add(new FieldComparisonResult(sourceField, targetFieldIndex[sourceField.FieldId]));
					if (abortOnChangeFound) return changedFields.ToArray();
				}
			}

			return changedFields.ToArray();
		}

		protected virtual bool IsFieldDifferent(IItemFieldValue sourceField, Dictionary<Guid, IItemFieldValue> targetFields, Guid fieldId)
		{
			// note that returning "true" means the values DO NOT MATCH EACH OTHER.

			if (sourceField == null) return false;

			// it's a "match" if the target item does not contain the source field
			IItemFieldValue targetField;
			if (!targetFields.TryGetValue(fieldId, out targetField)) return true;

			var fieldComparer = FieldComparers.FirstOrDefault(comparer => comparer.CanCompare(sourceField, targetField));

			if (fieldComparer == null) throw new InvalidOperationException("Unable to find a field comparison for " + sourceField.NameHint);

			return !fieldComparer.AreEqual(sourceField, targetField);
		}

		/// <summary>
		/// Helper method to get a specific version from a source item, if it exists
		/// </summary>
		/// <returns>Null if the version does not exist or the version if it exists</returns>
		protected virtual IItemVersion GetVersion(IItemData sourceItemData, string language, int versionNumber)
		{
			return sourceItemData.Versions.FirstOrDefault(x => x.Language.Name.Equals(language, StringComparison.OrdinalIgnoreCase) && x.VersionNumber == versionNumber);
		}
	}
}
