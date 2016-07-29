using System.Linq;
using Rainbow.Diff.Fields;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Diff
{
	public class ItemComparisonResult
	{
		public ItemComparisonResult(IItemData sourceItemData, IItemData targetItemData, bool isRenamed = false, bool isMoved = false, bool isTemplateChanged = false, bool isBranchChanged = false, FieldComparisonResult[] changedSharedFields = null, ItemVersionComparisonResult[] changedVersions = null, ItemLanguageComparisonResult[] changedUnversionedFields = null)
		{
			Assert.ArgumentNotNull(sourceItemData, "sourceItem");
			Assert.ArgumentNotNull(targetItemData, "targetItem");

			SourceItemData = sourceItemData;
			TargetItemData = targetItemData;
			IsRenamed = isRenamed;
			IsMoved = isMoved;
			IsTemplateChanged = isTemplateChanged;
			IsBranchChanged = isBranchChanged;
			ChangedSharedFields = changedSharedFields ?? new FieldComparisonResult[0];
			ChangedVersions = changedVersions ?? new ItemVersionComparisonResult[0];
			ChangedUnversionedFields = changedUnversionedFields ?? new ItemLanguageComparisonResult[0];
		}

		public IItemData SourceItemData { get; }
		public IItemData TargetItemData { get; }

		public virtual bool AreEqual
		{
			get
			{
				return !IsRenamed &&
						!IsTemplateChanged &&
						!IsBranchChanged &&
						!IsMoved &&
						ChangedSharedFields.Length == 0 &&
						ChangedUnversionedFields.Length == 0 &&
						ChangedVersions.Length == 0 &&
						SourceItemData != null &&
						TargetItemData != null &&
						ChangedVersions.All(version => version.SourceVersion != null && version.TargetVersion != null);
			}
		}

		public bool IsRenamed { get; }
		public bool IsMoved { get; }
		public bool IsTemplateChanged { get; }
		public bool IsBranchChanged { get; }
		public FieldComparisonResult[] ChangedSharedFields { get; }
		public ItemLanguageComparisonResult[] ChangedUnversionedFields { get; }
		public ItemVersionComparisonResult[] ChangedVersions { get; }
	}
}
