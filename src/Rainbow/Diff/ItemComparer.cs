using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Rainbow.Diff.Fields;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable LoopCanBeConvertedToQuery

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
		/// Compares two items, and as soon as one difference is found returns the result with that single difference present (does not perform complete comparison thereafter)
		/// </summary>
		public virtual ItemComparisonResult FastCompare(IItemData sourceItem, IItemData targetItem)
		{
			return CompareInternal(sourceItem, targetItem, true);
		}

		protected virtual ItemComparisonResult CompareInternal(IItemData sourceItem, IItemData targetItem, bool abortOnChangeFound)
		{
			Assert.ArgumentNotNull(targetItem, "targetItem");
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			bool moved = sourceItem.ParentId != targetItem.ParentId;

			// check if templates are different
			bool templateChanged = IsTemplateChanged(sourceItem, targetItem);

			if(abortOnChangeFound && templateChanged) return new ItemComparisonResult(sourceItem, targetItem, isTemplateChanged: true);

			// check if branches are different
			bool branchChanged = IsBranchChanged(sourceItem, targetItem);

			if (abortOnChangeFound && branchChanged) return new ItemComparisonResult(sourceItem, targetItem, isBranchChanged: true);

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


			var sourceUnversioned = sourceItem.UnversionedFields.ToDictionary(value => value.Language.Name);
			var targetUnversioned = targetItem.UnversionedFields.ToDictionary(value => value.Language.Name);
			var changedUnversionedFields = new List<ItemLanguageComparisonResult>();

			// check for unversioned fields only in source or target
			foreach (var source in sourceUnversioned)
			{
				// NOTE: empty values in the source are considered to be 'allowed to not exist in the target' (hence the .Any below to check for non empty values)
				// e.g. a field may exist in Sitecore that may not be serialized.
				if (!targetUnversioned.ContainsKey(source.Key) && source.Value.Fields.Any(field => !string.IsNullOrEmpty(field.Value)))
				{
					changedUnversionedFields.Add(new ItemLanguageComparisonResult(source.Value, source.Value.Fields.Select(field => new FieldComparisonResult(field, null)).ToArray()));

					if (abortOnChangeFound) return new ItemComparisonResult(sourceItem, targetItem, changedUnversionedFields: changedUnversionedFields.ToArray());
				}
			}

			foreach (var target in targetUnversioned)
			{
				if (!sourceUnversioned.ContainsKey(target.Key))
				{
					changedUnversionedFields.Add(new ItemLanguageComparisonResult(target.Value, target.Value.Fields.Select(field => new FieldComparisonResult(null, field)).ToArray()));

					if (abortOnChangeFound) return new ItemComparisonResult(sourceItem, targetItem, changedUnversionedFields: changedUnversionedFields.ToArray());
				}
			}

			// see if serialized unversioned values have any differences in the source data
			foreach (var source in sourceUnversioned)
			{
				if (!targetUnversioned.ContainsKey(source.Key)) continue;

				var target = targetUnversioned[source.Key];

				var changedFields = GetFieldDifferences(source.Value.Fields, target.Fields, sourceItem, targetItem, abortOnChangeFound);

				if (changedFields.Length > 0)
				{
					// field changes found
					changedUnversionedFields.Add(new ItemLanguageComparisonResult(source.Value, changedFields));

					if (abortOnChangeFound) break;
				}
			}

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

			return new ItemComparisonResult(sourceItem, targetItem, renamed, moved, templateChanged, branchChanged, changedSharedFields, changedVersions.ToArray(), changedUnversionedFields.ToArray());
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

		protected virtual bool IsBranchChanged(IItemData existingItemData, IItemData serializedItemData)
		{
			if (existingItemData.BranchId == default(Guid) && serializedItemData.BranchId == default(Guid)) return false;

			return !serializedItemData.BranchId.Equals(existingItemData.BranchId);
		}

		protected virtual FieldComparisonResult[] GetFieldDifferences(IEnumerable<IItemFieldValue> sourceFields, IEnumerable<IItemFieldValue> targetFields, IItemData existingItemData, IItemData serializedItemData, bool abortOnChangeFound)
		{
			var targetFieldIndex = targetFields.ToDictionary(x => x.FieldId);
			var sourceFieldIndex = sourceFields.ToDictionary(x => x.FieldId);

			var changedFields = new List<FieldComparisonResult>();

			bool isDifferent;
			IItemFieldValue targetField;
			IItemFieldValue sourceField;

			foreach (var sourceFieldKeypair in sourceFieldIndex)
			{
				sourceField = sourceFieldKeypair.Value;

				isDifferent = IsFieldDifferent(sourceField, targetFieldIndex, sourceField.FieldId);

				if (isDifferent)
				{
					if (targetFieldIndex.TryGetValue(sourceField.FieldId, out targetField))
					{
						changedFields.Add(new FieldComparisonResult(sourceField, targetField));
						if (abortOnChangeFound) return changedFields.ToArray();
					}
					// NOTE: empty values in the source are considered to be 'allowed to not exist in the target'
					// e.g. a field may exist in Sitecore that may not be serialized.
					else if (!string.IsNullOrEmpty(sourceField.Value))
					{
						changedFields.Add(new FieldComparisonResult(sourceField, null));
						if (abortOnChangeFound) return changedFields.ToArray();
					}
				}
			}

			// add target-only fields to the changes
			IItemFieldValue targetOnlyField;
			foreach (var targetFieldIndexKeypair in targetFieldIndex)
			{
				targetOnlyField = targetFieldIndexKeypair.Value;

				if (sourceFieldIndex.ContainsKey(targetOnlyField.FieldId)) continue;
				if (string.IsNullOrEmpty(targetOnlyField.Value)) continue;

				changedFields.Add(new FieldComparisonResult(null, targetOnlyField));
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

			IFieldComparer comparer;
			for (var index = 0; index < FieldComparers.Count; index++)
			{
				comparer = FieldComparers[index];

				if (!comparer.CanCompare(sourceField, targetField)) continue;

				return !comparer.AreEqual(sourceField, targetField);
			}

			throw new InvalidOperationException("Unable to find a field comparison for " + sourceField.NameHint);
		}

		/// <summary>
		/// Helper method to get a specific version from a source item, if it exists
		/// </summary>
		/// <returns>Null if the version does not exist or the version if it exists</returns>
		protected virtual IItemVersion GetVersion(IItemData sourceItemData, string language, int versionNumber)
		{
			foreach (var version in sourceItemData.Versions)
			{
				if (!version.Language.Name.Equals(language, StringComparison.OrdinalIgnoreCase)) continue;
				if (version.VersionNumber != versionNumber) continue;
				
				return version;
			}

			return null;
		}
	}
}
