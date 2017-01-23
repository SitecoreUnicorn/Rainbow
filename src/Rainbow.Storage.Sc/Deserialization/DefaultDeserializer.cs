using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
using Convert = System.Convert;
using Version = Sitecore.Data.Version;

namespace Rainbow.Storage.Sc.Deserialization
{
	/// <summary>
	/// This is a re-implementation of the way Sitecore does deserialization. Unlike the stock deserializer,
	/// this exposes much richer logging and only sets field values if they have changed (which is faster).
	/// It also enables Field Filter support, which allows ignoring deserialization of certain fields.
	/// </summary>
	public class DefaultDeserializer : IDeserializer
	{
		private readonly IDefaultDeserializerLogger _logger;
		private readonly IFieldFilter _fieldFilter;

		public IDataStore ParentDataStore { get; set; }

		public DefaultDeserializer(IDefaultDeserializerLogger logger, IFieldFilter fieldFilter)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");

			_logger = logger;
			_fieldFilter = fieldFilter;
		}

		public IItemData Deserialize(IItemData serializedItemData)
		{
			Assert.ArgumentNotNull(serializedItemData, "serializedItem");

			bool newItemWasCreated;
			var targetItem = GetOrCreateTargetItem(serializedItemData, out newItemWasCreated);

			var softErrors = new List<TemplateMissingFieldException>();

			try
			{
				ChangeTemplateIfNeeded(serializedItemData, targetItem);

				ChangeBranchIfNeeded(serializedItemData, targetItem, newItemWasCreated);

				RenameIfNeeded(serializedItemData, targetItem);

				ResetTemplateEngineIfItemIsTemplate(targetItem);

				UpdateFieldSharingIfNeeded(serializedItemData, targetItem);

				PasteSharedFields(serializedItemData, targetItem, newItemWasCreated, softErrors);

				PasteVersions(serializedItemData, targetItem, newItemWasCreated, softErrors);

				PasteUnversionedFields(serializedItemData, targetItem, newItemWasCreated, softErrors);

				if (softErrors.Count > 0) throw TemplateMissingFieldException.Merge(softErrors);

				return new ItemData(targetItem, ParentDataStore);
			}
			catch (ParentForMovedItemNotFoundException)
			{
				throw;
			}
			catch (ParentItemNotFoundException)
			{
				throw;
			}
			catch (TemplateMissingFieldException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (newItemWasCreated)
				{
					targetItem.Delete();
					ClearCaches(targetItem.Database, new ID(serializedItemData.Id));
				}

				throw new DeserializationException("Failed to paste item: " + serializedItemData.Path, ex);
			}
		}

		protected virtual Item GetOrCreateTargetItem(IItemData serializedItemData, out bool newItemWasCreated)
		{
			Database database = Factory.GetDatabase(serializedItemData.DatabaseName);

			Item destinationParentItem = database.GetItem(new ID(serializedItemData.ParentId));
			Item targetItem = database.GetItem(new ID(serializedItemData.Id));

			newItemWasCreated = false;

			// very occasionally the caches will be out of date, and can return that an item 'exists' but
			// has no parent. If this occurs, we want to dump all caches and return the item, which will
			// then correctly reflect that the item did not exist and create it.
			// Why? Not sure.
			if (targetItem != null && targetItem.ParentID == ID.Null)
			{
				CacheManager.ClearAllCaches();

				targetItem = database.GetItem(new ID(serializedItemData.Id));
			}

			// the target item did not yet exist, so we need to start by creating it
			if (targetItem == null)
			{
				targetItem = CreateTargetItem(serializedItemData, destinationParentItem);

				_logger.CreatedNewItem(targetItem);

				newItemWasCreated = true;
			}
			else
			{
				// check if the parent of the serialized item does not exist
				// which, since the target item is known to exist, means that
				// the serialized item was moved but its new parent item does
				// not exist to paste it under
				if (destinationParentItem == null)
				{
					throw new ParentForMovedItemNotFoundException
					{
						ParentID = new ID(serializedItemData.ParentId).ToString(),
						Item = targetItem
					};
				}

				// if the parent IDs mismatch that means we need to move the existing
				// target item to its new parent from the serialized item
				if (destinationParentItem.ID != targetItem.ParentID)
				{
					var oldParent = targetItem.Parent;
					targetItem.MoveTo(destinationParentItem);

					if (!targetItem.ParentID.IsNull)
						_logger.MovedItemToNewParent(destinationParentItem, oldParent, targetItem);
				}
			}
			return targetItem;
		}

		protected void ChangeBranchIfNeeded(IItemData serializedItemData, Item targetItem, bool newItemWasCreated)
		{
			if (targetItem.BranchId.Guid.Equals(serializedItemData.BranchId)) return;

			Guid oldBranchId = targetItem.BranchId.Guid;

			targetItem.Editing.BeginEdit();
			targetItem.RuntimeSettings.ReadOnlyStatistics = true;
			targetItem.BranchId = ID.Parse(serializedItemData.BranchId);
			targetItem.Editing.EndEdit();

			ClearCaches(targetItem.Database, targetItem.ID);
			targetItem.Reload();

			if (oldBranchId != serializedItemData.BranchId && !newItemWasCreated)
			{
				_logger.ChangedBranchTemplate(targetItem, new ID(oldBranchId).ToString());
			}
		}

		protected void RenameIfNeeded(IItemData serializedItemData, Item targetItem)
		{
			if (targetItem.Name.Equals(serializedItemData.Name, StringComparison.Ordinal)) return;

			string oldName = targetItem.Name;

			targetItem.Editing.BeginEdit();
			targetItem.RuntimeSettings.ReadOnlyStatistics = true;
			targetItem.Name = serializedItemData.Name;
			targetItem.Editing.EndEdit();

			ClearCaches(targetItem.Database, targetItem.ID);
			targetItem.Reload();

			if (oldName != serializedItemData.Name)
				_logger.RenamedItem(targetItem, oldName);
		}

		protected void ChangeTemplateIfNeeded(IItemData serializedItemData, Item targetItem)
		{
			if (targetItem.TemplateID.Guid == serializedItemData.TemplateId) return;

			var oldTemplate = targetItem.Template;
			var newTemplate = targetItem.Database.Templates[new ID(serializedItemData.TemplateId)];

			Assert.IsNotNull(newTemplate, "Cannot change template of {0} because its new template {1} does not exist!", targetItem.ID, serializedItemData.TemplateId);

			targetItem.Editing.BeginEdit();

			targetItem.RuntimeSettings.ReadOnlyStatistics = true;
			try
			{
				targetItem.ChangeTemplate(newTemplate);
			}
			catch
			{
				// this generally means that we tried to sync an item and change its template AND we already deleted the item's old template in the same sync
				// the Sitecore change template API chokes if the item's CURRENT template is unavailable, but we can get around that
				// issure reported to Sitecore Support (406546)
				lock (targetItem.SyncRoot)
				{
					Template sourceTemplate = TemplateManager.GetTemplate(targetItem);
					Template targetTemplate = TemplateManager.GetTemplate(newTemplate.ID, targetItem.Database);

					Error.AssertNotNull(targetTemplate, "Could not get target in ChangeTemplate");

					// this is probably true if we got here. This is the check the Sitecore API fails to make, and throws a NullReferenceException.
					if (sourceTemplate == null) sourceTemplate = targetTemplate;

					TemplateChangeList templateChangeList = sourceTemplate.GetTemplateChangeList(targetTemplate);
					TemplateManager.ChangeTemplate(targetItem, templateChangeList);
				}
			}

			targetItem.Editing.EndEdit();

			ClearCaches(targetItem.Database, targetItem.ID);
			targetItem.Reload();

			_logger.ChangedTemplate(targetItem, oldTemplate);
		}

		/// <summary>
		/// When you change a template field from versioned to shared or unversioned (or vice versa), the conversion of the values
		/// in existing items with that field is NOT automatic. This method causes the requisite field updates (moving the values between db tables)
		/// </summary>
		protected void UpdateFieldSharingIfNeeded(IItemData serializedItemData, Item targetItem)
		{
			Assert.ArgumentNotNull(serializedItemData, nameof(serializedItemData));
			Assert.ArgumentNotNull(targetItem, nameof(targetItem));

			if (serializedItemData.TemplateId != TemplateIDs.TemplateField.Guid) return;

			var shared = serializedItemData.SharedFields.FirstOrDefault(field => field.FieldId == TemplateFieldIDs.Shared.Guid);
			var unversioned = serializedItemData.SharedFields.FirstOrDefault(field => field.FieldId == TemplateFieldIDs.Unversioned.Guid);

			TemplateFieldSharing sharedness = TemplateFieldSharing.None;

			if (shared != null && shared.Value.Equals("1")) sharedness = TemplateFieldSharing.Shared;
			else if (unversioned != null && unversioned.Value.Equals("1")) sharedness = TemplateFieldSharing.Unversioned;

			var templateField = TemplateManager.GetTemplateField(targetItem.ID, targetItem.Parent.Parent.ID, targetItem.Database);

			if (templateField == null)
			{
				// BOMB WIG
				CacheManager.ClearAllCaches();
				targetItem.Database.Engines.TemplateEngine.Reset();

				templateField = TemplateManager.GetTemplateField(targetItem.ID, targetItem.Parent.Parent.ID, targetItem.Database);
			}

			Assert.IsNotNull(templateField, $"Unable to find template field {targetItem.ID} in {targetItem.Database.Name}, even after resetting the engine.");

			TemplateFieldSharing templateSharedness = TemplateFieldSharing.None;
			if (templateField.IsShared) templateSharedness = TemplateFieldSharing.Shared;
			else if (templateField.IsUnversioned) templateSharedness = TemplateFieldSharing.Unversioned;

			if (templateSharedness == sharedness) return;

			TemplateManager.ChangeFieldSharing(templateField, sharedness, targetItem.Database);
		}

		protected Item CreateTargetItem(IItemData serializedItemData, Item destinationParentItem)
		{
			Database database = Factory.GetDatabase(serializedItemData.DatabaseName);
			if (destinationParentItem == null)
			{
				throw new ParentItemNotFoundException
				{
					ParentID = new ID(serializedItemData.ParentId).ToString(),
					ItemID = new ID(serializedItemData.Id).ToString()
				};
			}

			AssertTemplate(database, new ID(serializedItemData.TemplateId), serializedItemData.Path);

			Item targetItem = ItemManager.AddFromTemplate(serializedItemData.Name, new ID(serializedItemData.TemplateId), destinationParentItem, new ID(serializedItemData.Id));

			if (targetItem == null)
				throw new DeserializationException("Creating " + serializedItemData.DatabaseName + ":" + serializedItemData.Path + " failed. API returned null.");

			targetItem.Versions.RemoveAll(true);

			return targetItem;
		}

		protected virtual void PasteSharedFields(IItemData serializedItemData, Item targetItem, bool newItemWasCreated, List<TemplateMissingFieldException> softErrors)
		{
			bool commitEdit = false;

			try
			{
				targetItem.Editing.BeginEdit();

				targetItem.RuntimeSettings.ReadOnlyStatistics = true;
				targetItem.RuntimeSettings.SaveAll = true;

				var allTargetSharedFields = serializedItemData.SharedFields.ToLookup(field => field.FieldId);

				foreach (Field field in targetItem.Fields)
				{
					if (field.Shared && !allTargetSharedFields.Contains(field.ID.Guid))
					{
						_logger.ResetFieldThatDidNotExistInSerialized(field);

						field.Reset();

						commitEdit = true;
					}
				}

				foreach (var field in serializedItemData.SharedFields)
				{
					try
					{
						if (PasteField(targetItem, field, newItemWasCreated))
							commitEdit = true;
					}
					catch (TemplateMissingFieldException tex)
					{
						softErrors.Add(tex);
					}
				}

				// we commit the edit context - and write to the DB - only if we changed something
				if (commitEdit)
				{
					targetItem.Editing.EndEdit();

					ClearCaches(targetItem.Database, new ID(serializedItemData.Id));

					targetItem.Reload();

					ResetTemplateEngineIfItemIsTemplate(targetItem);
				}
			}
			finally
			{
				if (targetItem.Editing.IsEditing)
					targetItem.Editing.CancelEdit();
			}
		}

		protected virtual void PasteVersions(IItemData serializedItemData, Item targetItem, bool newItemWasCreated, List<TemplateMissingFieldException> softErrors)
		{
			Hashtable versionTable = CommonUtils.CreateCIHashtable();

			// this version table allows us to detect and remove orphaned versions that are not in the
			// serialized version, but are in the database version
			foreach (Item version in targetItem.Versions.GetVersions(true))
				versionTable[version.Uri] = null;

			foreach (var syncVersion in serializedItemData.Versions)
			{
				var version = PasteVersion(targetItem, syncVersion, newItemWasCreated, softErrors);
				if (versionTable.ContainsKey(version.Uri))
					versionTable.Remove(version.Uri);
			}

			foreach (ItemUri uri in versionTable.Keys)
			{
				var versionToRemove = Database.GetItem(uri);

				_logger.RemovingOrphanedVersion(versionToRemove);

				versionToRemove.Versions.RemoveVersion();
			}
		}

		protected virtual Item PasteVersion(Item item, IItemVersion serializedVersion, bool creatingNewItem, List<TemplateMissingFieldException> softErrors)
		{
			Language language = Language.Parse(serializedVersion.Language.Name);
			var targetVersion = Version.Parse(serializedVersion.VersionNumber);

			Item languageItem = item.Database.GetItem(item.ID, language);

			if(languageItem == null) throw new InvalidOperationException("Item retrieved from the database was null. This may indicate issues with the Sitecore cache. Recycle your app pool and try again and it should work.");

			Item languageVersionItem = languageItem.Versions[targetVersion];

			IDictionary<Guid, IItemFieldValue> serializedVersionFieldsLookup = serializedVersion.Fields.ToDictionary(field => field.FieldId);

			// Add a new version if we need to for the serialized version we're pasting
			// NOTE: normally Sitecore will return an empty version when requested (e.g. you can ask for 'en#25' and get a blank version back)
			// so this null check is only here for legacy support
			if (languageVersionItem == null)
			{
				languageVersionItem = languageItem.Versions.AddVersion();

				if (languageVersionItem == null) throw new InvalidOperationException("Failed to add a new version: AddVersion() returned null.");

				if (!creatingNewItem)
					_logger.AddedNewVersion(languageVersionItem);
			}

			// Based on the note above, in order to detect the modern form where item.Versions[] returns a blank version no matter what,
			// we need to check again to see if the version we requested is actually currently present on the item or not, so we can correctly log version adds
			// ReSharper disable once SimplifyLinqExpression
			if (!languageVersionItem.Versions.GetVersionNumbers().Any(x => x.Number == languageVersionItem.Version.Number))
			{
				if (!creatingNewItem)
					_logger.AddedNewVersion(languageVersionItem);
			}

			// begin writing the version data
			bool commitEdit = false;

			try
			{
				languageVersionItem.Editing.BeginEdit();

				languageVersionItem.RuntimeSettings.ReadOnlyStatistics = true;

				if (languageVersionItem.Versions.Count == 0)
					languageVersionItem.Fields.ReadAll();

				// find versioned fields that need to be reset to standard value, because they are not in serialized
				// (we do all these checks so we can back out of the edit context and avoid a DB write if we don't need one)
				foreach (Field field in languageVersionItem.Fields)
				{
					// shared/unversioned fields = ignore, those are handled in their own paste methods
					if (field.Shared || field.Unversioned) continue;

					// if we have a value in the serialized item, we don't need to reset the field
					if (serializedVersionFieldsLookup.ContainsKey(field.ID.Guid)) continue;

					// if the field is one of revision, updated, or updated by we can specially ignore it, because these will get set below if actual field changes occur
					// so there's no need to reset them as well
					if (field.ID == FieldIDs.Revision || field.ID == FieldIDs.UpdatedBy || field.ID == FieldIDs.Updated) continue;

					_logger.ResetFieldThatDidNotExistInSerialized(field);

					field.Reset();

					commitEdit = true;
				}

				bool wasOwnerFieldParsed = false;
				foreach (IItemFieldValue field in serializedVersion.Fields)
				{
					if (field.FieldId == FieldIDs.Owner.Guid)
						wasOwnerFieldParsed = true;

					try
					{
						if (PasteField(languageVersionItem, field, creatingNewItem))
							commitEdit = true;
					}
					catch (TemplateMissingFieldException tex)
					{
						softErrors.Add(tex);
					}
				}

				if (!wasOwnerFieldParsed)
				{
					languageVersionItem.Fields[FieldIDs.Owner].Reset();
					commitEdit = true;
				}

				// if the item came with blank statistics, and we're creating the item or have version updates already, let's set some sane defaults - update revision, set last updated, etc
				if (creatingNewItem || commitEdit)
				{
					if (!serializedVersionFieldsLookup.ContainsKey(FieldIDs.Revision.Guid))
					{
						languageVersionItem.Fields[FieldIDs.Revision].SetValue(Guid.NewGuid().ToString(), true);
					}

					if (!serializedVersionFieldsLookup.ContainsKey(FieldIDs.Updated.Guid))
					{
						languageVersionItem[FieldIDs.Updated] = DateUtil.ToIsoDate(DateTime.UtcNow);
					}

					if (!serializedVersionFieldsLookup.ContainsKey(FieldIDs.UpdatedBy.Guid))
					{
						languageVersionItem[FieldIDs.UpdatedBy] = @"sitecore\unicorn";
					}
				}

				// we commit the edit context - and write to the DB - only if we changed something
				if (commitEdit)
					languageVersionItem.Editing.EndEdit();
			}
			finally
			{
				if (languageVersionItem.Editing.IsEditing)
					languageVersionItem.Editing.CancelEdit();
			}

			if (commitEdit)
			{
				ClearCaches(languageVersionItem.Database, languageVersionItem.ID);
				ResetTemplateEngineIfItemIsTemplate(languageVersionItem);
			}

			return languageVersionItem;
		}

		protected virtual void PasteUnversionedFields(IItemData serializedItemData, Item targetItem, bool newItemWasCreated, List<TemplateMissingFieldException> softErrors)
		{
			foreach (var language in serializedItemData.UnversionedFields)
			{
				PasteUnversionedLanguage(targetItem, language, newItemWasCreated, softErrors);
			}
		}

		protected virtual void PasteUnversionedLanguage(Item item, IItemLanguage serializedLanguage, bool newItemWasCreated, List<TemplateMissingFieldException> softErrors)
		{
			Language language = Language.Parse(serializedLanguage.Language.Name);

			Item targetItem = item.Database.GetItem(item.ID, language);

			if (targetItem == null)
			{
				// this can occasionally occur when syncing a tpSync config
				CacheManager.ClearAllCaches();
				targetItem = item.Database.GetItem(item.ID, language);
			}

			Assert.IsNotNull(targetItem, "Target item language to paste unversioned fields into was null.");

			bool commitEdit = false;

			try
			{
				targetItem.Editing.BeginEdit();

				targetItem.RuntimeSettings.ReadOnlyStatistics = true;
				targetItem.RuntimeSettings.SaveAll = true;

				var allTargetUnversionedFields = serializedLanguage.Fields.ToLookup(field => field.FieldId);

				foreach (Field field in targetItem.Fields)
				{
					// field was not serialized. Which means the field is either blank or has its standard value, so let's reset it
					if (field.Unversioned && !field.Shared && !allTargetUnversionedFields.Contains(field.ID.Guid))
					{
						_logger.ResetFieldThatDidNotExistInSerialized(field);

						field.Reset();

						commitEdit = true;
					}
				}

				foreach (var field in serializedLanguage.Fields)
				{
					try
					{
						if (PasteField(targetItem, field, newItemWasCreated))
							commitEdit = true;
					}
					catch (TemplateMissingFieldException tex)
					{
						softErrors.Add(tex);
					}
				}

				// we commit the edit context - and write to the DB - only if we changed something
				if (commitEdit)
					targetItem.Editing.EndEdit();
			}
			finally
			{
				if (targetItem.Editing.IsEditing)
					targetItem.Editing.CancelEdit();
			}
		}

		/// <summary>
		/// Inserts field value into item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="field">The field.</param>
		/// <param name="creatingNewItem">Whether the item under update is new or not (controls logging verbosity)</param>
		/// <exception cref="T:Sitecore.Data.Serialization.Exceptions.FieldIsMissingFromTemplateException"/>
		protected virtual bool PasteField(Item item, IItemFieldValue field, bool creatingNewItem)
		{
			if (!_fieldFilter.Includes(field.FieldId))
			{
				_logger.SkippedPastingIgnoredField(item, field);
				return false;
			}

			Template template = AssertTemplate(item.Database, item.TemplateID, item.Paths.Path);
			if (template.GetField(new ID(field.FieldId)) == null)
			{
				item.Database.Engines.TemplateEngine.Reset();
				template = AssertTemplate(item.Database, item.TemplateID, item.Paths.Path);
			}

			if (template.GetField(new ID(field.FieldId)) == null)
			{
				throw new TemplateMissingFieldException(item.TemplateName, item.Database.Name + ":" + item.Paths.FullPath, field);
			}

			Field itemField = item.Fields[new ID(field.FieldId)];
			if (itemField.IsBlobField)
			{
				Guid existingBlobId;
				bool hasExistingId = Guid.TryParse(itemField.Value, out existingBlobId);

				// serialized blob has no value (media item with detached media)
				if (!field.BlobId.HasValue)
				{
					if (!hasExistingId || existingBlobId == Guid.Empty) return false; // no blob ID and none in DB either, so we're cool

					// existing blob in DB but none in serialized
					// so clear the blob and field value
					ItemManager.RemoveBlobStream(existingBlobId, item.Database);
					itemField.SetValue(string.Empty, true);
					_logger.WroteBlobStream(item, field);

					return true;
				}

				// check if existing blob is here with the same ID; abort if so
				if (hasExistingId && existingBlobId == field.BlobId.Value) return false;

				byte[] buffer = Convert.FromBase64String(field.Value);

				// if an existing blob of a different ID exists, drop it from the database
				if (existingBlobId != default(Guid)) ItemManager.RemoveBlobStream(existingBlobId, item.Database);

				// write the new blob to the database
				ItemManager.SetBlobStream(new MemoryStream(buffer, false), field.BlobId.Value, item.Database);

				// set the value of the blob field to the correct blob ID
				itemField.SetValue(MainUtil.GuidToString(field.BlobId.Value), true);

				if (!creatingNewItem)
					_logger.WroteBlobStream(item, field);

				return true;
			}

			if (field.Value != null && !field.Value.Equals(itemField.GetValue(false, false)))
			{
				var oldValue = itemField.Value;
				itemField.SetValue(field.Value, true);

				if (!creatingNewItem)
					_logger.UpdatedChangedFieldValue(item, field, oldValue);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes information about a specific item from database caches. This compensates
		/// cache functionality that depends on database events (which are disabled when loading).
		/// </summary>
		/// <param name="database">Database to clear caches for.</param>
		/// <param name="itemId">Item ID to remove</param>
		protected virtual void ClearCaches(Database database, ID itemId)
		{
			database.Caches.ItemCache.RemoveItem(itemId);
			database.Caches.DataCache.RemoveItemInformation(itemId);
		}

		protected virtual void ResetTemplateEngineIfItemIsTemplate(Item target)
		{
			if (!target.Database.Engines.TemplateEngine.IsTemplatePart(target))
				return;

			target.Database.Engines.TemplateEngine.Reset();
		}

		/// <summary>
		/// Asserts that the template is present in the database.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="templateId">The template.</param>
		/// <param name="itemPath">The path to the item being asserted (used if an error occurs)</param>
		/// <returns>
		/// The template being asserted.
		/// </returns>
		protected virtual Template AssertTemplate(Database database, ID templateId, string itemPath)
		{
			Template template = database.Engines.TemplateEngine.GetTemplate(templateId);
			if (template == null)
			{
				database.Engines.TemplateEngine.Reset();
				template = database.Engines.TemplateEngine.GetTemplate(templateId);
			}

			if (template == null)
				throw new DeserializationException("Template {0} for item {1} not found".FormatWith(templateId, itemPath));

			return template;
		}
	}
}
