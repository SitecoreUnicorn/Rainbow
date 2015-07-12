using Rainbow.Diff.Fields;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Diff
{
	public class ItemComparisonResult
	{
		public ItemComparisonResult(ISerializableItem sourceItem, ISerializableItem targetItem, bool isRenamed = false, bool isMoved = false, bool isTemplateChanged = false, FieldComparisonResult[] changedSharedFields = null, ItemVersionComparisonResult[] changedVersions = null)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(targetItem, "targetItem");

			SourceItem = sourceItem;
			TargetItem = targetItem;
			IsRenamed = isRenamed;
			IsMoved = isMoved;
			IsTemplateChanged = isTemplateChanged;
			ChangedSharedFields = changedSharedFields ?? new FieldComparisonResult[0];
			ChangedVersions = changedVersions ?? new ItemVersionComparisonResult[0];
		}

		public ISerializableItem SourceItem { get; private set; }
		public ISerializableItem TargetItem { get; private set; }
		public bool AreEqual { get { return IsRenamed || IsTemplateChanged || ChangedSharedFields.Length > 0 || ChangedVersions.Length > 0 || SourceItem == null || TargetItem == null; } }
		public bool IsRenamed { get; private set; }
		public bool IsMoved { get; private set; }
		public bool IsTemplateChanged { get; private set; }
		public FieldComparisonResult[] ChangedSharedFields { get; private set; }
		public ItemVersionComparisonResult[] ChangedVersions { get; private set; }
	}
}
