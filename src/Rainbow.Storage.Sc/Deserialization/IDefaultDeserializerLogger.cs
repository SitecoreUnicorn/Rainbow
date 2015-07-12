using Rainbow.Model;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Rainbow.Storage.Sc.Deserialization
{
	public interface IDefaultDeserializerLogger
	{
		void CreatedNewItem(Item targetItem);

		void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem);

		void RemovingOrphanedVersion(Item versionToRemove);

		void RenamedItem(Item targetItem, string oldName);

		void ChangedBranchTemplate(Item targetItem, string oldBranchId);

		void ChangedTemplate(Item targetItem, TemplateItem oldTemplate);

		void AddedNewVersion(Item newVersion);

		void SkippedMissingTemplateField(Item item, IItemFieldValue field);

		void WroteBlobStream(Item item, IItemFieldValue field);

		void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue);

		void ResetFieldThatDidNotExistInSerialized(Field field);

		void SkippedPastingIgnoredField(Item item, IItemFieldValue field);
	}
}
