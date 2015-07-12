using System.Linq;
using Rainbow.Diff.Fields;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Diff
{
	public class ItemComparisonResult
	{
		public ItemComparisonResult(IItemData sourceItemData, IItemData targetItemData, bool isRenamed = false, bool isMoved = false, bool isTemplateChanged = false, FieldComparisonResult[] changedSharedFields = null, ItemVersionComparisonResult[] changedVersions = null)
		{
			Assert.ArgumentNotNull(sourceItemData, "sourceItem");
			Assert.ArgumentNotNull(targetItemData, "targetItem");

			SourceItemData = sourceItemData;
			TargetItemData = targetItemData;
			IsRenamed = isRenamed;
			IsMoved = isMoved;
			IsTemplateChanged = isTemplateChanged;
			ChangedSharedFields = changedSharedFields ?? new FieldComparisonResult[0];
			ChangedVersions = changedVersions ?? new ItemVersionComparisonResult[0];
		}

		public IItemData SourceItemData { get; private set; }
		public IItemData TargetItemData { get; private set; }
		public bool AreEqual
		{
			get
			{
				return !IsRenamed &&
						!IsTemplateChanged &&
						!IsMoved &&
						ChangedSharedFields.Length == 0 &&
						ChangedVersions.Length == 0 &&
						SourceItemData != null &&
						TargetItemData != null &&
						ChangedVersions.All(version => version.SourceVersion != null && version.TargetVersion != null);
			}
		}

		public bool IsRenamed { get; private set; }
		public bool IsMoved { get; private set; }
		public bool IsTemplateChanged { get; private set; }
		public FieldComparisonResult[] ChangedSharedFields { get; private set; }
		public ItemVersionComparisonResult[] ChangedVersions { get; private set; }
	}
}
