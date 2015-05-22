using Gibson.Model;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Gibson.Deserialization
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

		void SkippedMissingTemplateField(Item item, ISerializableFieldValue field);

		void WroteBlobStream(Item item, ISerializableFieldValue field);

		void UpdatedChangedFieldValue(Item item, ISerializableFieldValue field, string oldValue);

		void ResetFieldThatDidNotExistInSerialized(Field field);

		void SkippedPastingIgnoredField(Item item, ISerializableFieldValue field);
	}
}
