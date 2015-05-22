using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gibson.Model;
using Gibson.Predicates;
using Sitecore;
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

namespace Gibson.Deserialization
{
	/// <summary>
	/// This is a re-implementation of the way Sitecore does deserialization. Unlike the stock deserializer,
	/// this exposes much richer logging and only sets field values if they have changed (which is faster).
	/// It also enables FieldPredicate support, which allows ignoring deserialization of certain fields.
	/// </summary>
	public class DefaultDeserializer : IDeserializer
	{
		private readonly IDefaultDeserializerLogger _logger;
		private readonly IFieldPredicate _fieldPredicate;

		public DefaultDeserializer(IDefaultDeserializerLogger logger, IFieldPredicate fieldPredicate)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(fieldPredicate, "fieldPredicate");

			_logger = logger;
			_fieldPredicate = fieldPredicate;
		}

		public ISerializableItem Deserialize(ISerializableItem serializedItem, bool ignoreMissingTemplateFields)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			Database database = Factory.GetDatabase(serializedItem.DatabaseName);

			Item destinationParentItem = database.GetItem(new ID(serializedItem.ParentId));
			Item targetItem = database.GetItem(new ID(serializedItem.Id));
			bool newItemWasCreated = false;

			// the target item did not yet exist, so we need to start by creating it
			if (targetItem == null)
			{
				targetItem = CreateTargetItem(serializedItem, destinationParentItem);

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
						ParentID = new ID(serializedItem.ParentId).ToString(),
						Item = targetItem
					};
				}

				// if the parent IDs mismatch that means we need to move the existing
				// target item to its new parent from the serialized item
				if (destinationParentItem.ID != targetItem.ParentID)
				{
					var oldParent = targetItem.Parent;
					targetItem.MoveTo(destinationParentItem);
					_logger.MovedItemToNewParent(destinationParentItem, oldParent, targetItem);
				}
			}
			try
			{
				ChangeTemplateIfNeeded(serializedItem, targetItem);
				RenameIfNeeded(serializedItem, targetItem);
				ResetTemplateEngineIfItemIsTemplate(targetItem);

				using (new EditContext(targetItem))
				{
					targetItem.RuntimeSettings.ReadOnlyStatistics = true;
					targetItem.RuntimeSettings.SaveAll = true;

					foreach (Field field in targetItem.Fields)
					{
						if (field.Shared && serializedItem.SharedFields.All(x => x.FieldId != field.ID.Guid))
						{
							_logger.ResetFieldThatDidNotExistInSerialized(field);
							field.Reset();
						}
					}

					foreach (var field in serializedItem.SharedFields)
						PasteSyncField(targetItem, field, ignoreMissingTemplateFields, newItemWasCreated);
				}

				ClearCaches(database, new ID(serializedItem.Id));
				targetItem.Reload();
				ResetTemplateEngineIfItemIsTemplate(targetItem);

				Hashtable versionTable = CommonUtils.CreateCIHashtable();

				// this version table allows us to detect and remove orphaned versions that are not in the
				// serialized version, but are in the database version
				foreach (Item version in targetItem.Versions.GetVersions(true))
					versionTable[version.Uri] = null;

				foreach (var syncVersion in serializedItem.Versions)
				{
					var version = PasteSyncVersion(targetItem, syncVersion, ignoreMissingTemplateFields, newItemWasCreated);
					if (versionTable.ContainsKey(version.Uri))
						versionTable.Remove(version.Uri);
				}

				foreach (ItemUri uri in versionTable.Keys)
				{
					var versionToRemove = Database.GetItem(uri);

					_logger.RemovingOrphanedVersion(versionToRemove);

					versionToRemove.Versions.RemoveVersion();
				}

				ClearCaches(targetItem.Database, targetItem.ID);

				return new SerializableItem(targetItem);
			}
			catch (ParentForMovedItemNotFoundException)
			{
				throw;
			}
			catch (ParentItemNotFoundException)
			{
				throw;
			}
#if SITECORE_7
			catch (FieldIsMissingFromTemplateException)
			{
				throw;
			}
#endif
			catch (Exception ex)
			{
				if (newItemWasCreated)
				{
					targetItem.Delete();
					ClearCaches(database, new ID(serializedItem.Id));
				}
				throw new Exception("Failed to paste item: " + serializedItem.Path, ex);
			}
		}

		protected void RenameIfNeeded(ISerializableItem serializedItem, Item targetItem)
		{
			if (targetItem.Name != serializedItem.Name || targetItem.BranchId.Guid != serializedItem.BranchId)
			{
				string oldName = targetItem.Name;
				Guid oldBranchId = targetItem.BranchId.Guid;

				using (new EditContext(targetItem))
				{
					targetItem.RuntimeSettings.ReadOnlyStatistics = true;
					targetItem.Name = serializedItem.Name;
					targetItem.BranchId = ID.Parse(serializedItem.BranchId);
				}

				ClearCaches(targetItem.Database, targetItem.ID);
				targetItem.Reload();

				if (oldName != serializedItem.Name)
					_logger.RenamedItem(targetItem, oldName);

				if (oldBranchId != serializedItem.BranchId)
					_logger.ChangedBranchTemplate(targetItem, new ID(oldBranchId).ToString());
			}
		}

		protected void ChangeTemplateIfNeeded(ISerializableItem serializedItem, Item targetItem)
		{
			if (targetItem.TemplateID.Guid != serializedItem.TemplateId)
			{
				var oldTemplate = targetItem.Template;
				var newTemplate = targetItem.Database.Templates[new ID(serializedItem.TemplateId)];

				Assert.IsNotNull(newTemplate, "Cannot change template of {0} because its new template {1} does not exist!", targetItem.ID, serializedItem.TemplateId);

				using (new EditContext(targetItem))
				{
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
				}

				ClearCaches(targetItem.Database, targetItem.ID);
				targetItem.Reload();

				_logger.ChangedTemplate(targetItem, oldTemplate);
			}
		}

		protected Item CreateTargetItem(ISerializableItem serializedItem, Item destinationParentItem)
		{
			Database database = Factory.GetDatabase(serializedItem.DatabaseName);
			if (destinationParentItem == null)
			{
				throw new ParentItemNotFoundException
				{
					ParentID = new ID(serializedItem.ParentId).ToString(),
					ItemID = new ID(serializedItem.Id).ToString()
				};
			}

			AssertTemplate(database, new ID(serializedItem.TemplateId), serializedItem.Path);

			Item targetItem = ItemManager.AddFromTemplate(serializedItem.Name, new ID(serializedItem.TemplateId), destinationParentItem, new ID(serializedItem.TemplateId));

			if(targetItem == null)
				throw new DeserializationException("Creating " + serializedItem.DatabaseName + ":" + serializedItem.Path + " failed. API returned null.");

			targetItem.Versions.RemoveAll(true);

			return targetItem;
		}

		protected virtual Item PasteSyncVersion(Item item, ISerializableVersion serializedVersion, bool ignoreMissingTemplateFields, bool creatingNewItem)
		{
			Language language = Language.Parse(serializedVersion.Language.Name);
			var targetVersion = Version.Parse(serializedVersion.VersionNumber);
			Item languageItem = item.Database.GetItem(item.ID, language);
			Item languageVersionItem = languageItem.Versions[targetVersion];

			if (languageVersionItem == null)
			{
				languageVersionItem = languageItem.Versions.AddVersion();
				if (!creatingNewItem)
					_logger.AddedNewVersion(languageVersionItem);
			}

			// ReSharper disable once SimplifyLinqExpression
			if (!languageVersionItem.Versions.GetVersionNumbers().Any(x => x.Number == languageVersionItem.Version.Number))
			{
				if (!creatingNewItem)
					_logger.AddedNewVersion(languageVersionItem);
			}

			using (new EditContext(languageVersionItem))
			{
				languageVersionItem.RuntimeSettings.ReadOnlyStatistics = true;

				if (languageVersionItem.Versions.Count == 0)
					languageVersionItem.Fields.ReadAll();

				foreach (Field field in languageVersionItem.Fields)
				{
					if (!field.Shared && serializedVersion.Fields.All(x => x.FieldId != field.ID.Guid))
					{
						_logger.ResetFieldThatDidNotExistInSerialized(field);
						field.Reset();
					}
				}

				bool wasOwnerFieldParsed = false;
				foreach (ISerializableFieldValue field in serializedVersion.Fields)
				{
					if (field.FieldId == FieldIDs.Owner.Guid)
						wasOwnerFieldParsed = true;

					PasteSyncField(languageVersionItem, field, ignoreMissingTemplateFields, creatingNewItem);
				}

				if (!wasOwnerFieldParsed)
					languageVersionItem.Fields[FieldIDs.Owner].Reset();
			}

			ClearCaches(languageVersionItem.Database, languageVersionItem.ID);
			ResetTemplateEngineIfItemIsTemplate(languageVersionItem);

			return languageVersionItem;
		}

		/// <summary>
		/// Inserts field value into item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="field">The field.</param>
		/// <param name="ignoreMissingTemplateFields">Whether to ignore fields in the serialized item that do not exist on the Sitecore template</param>
		/// <param name="creatingNewItem">Whether the item under update is new or not (controls logging verbosity)</param>
		/// <exception cref="T:Sitecore.Data.Serialization.Exceptions.FieldIsMissingFromTemplateException"/>
		protected virtual void PasteSyncField(Item item, ISerializableFieldValue field, bool ignoreMissingTemplateFields, bool creatingNewItem)
		{
			if (!_fieldPredicate.Includes(field.FieldId).IsIncluded)
			{
				//TODO _logger.SkippedPastingIgnoredField(item, field);
				return;
			}

			Template template = AssertTemplate(item.Database, item.TemplateID, item.Paths.Path);
			if (template.GetField(new ID(field.FieldId)) == null)
			{
				item.Database.Engines.TemplateEngine.Reset();
				template = AssertTemplate(item.Database, item.TemplateID, item.Paths.Path);
			}

			if (template.GetField(new ID(field.FieldId)) == null)
			{
				if (!ignoreMissingTemplateFields)
				{
#if SITECORE_7
					throw new FieldIsMissingFromTemplateException("Field " + field.FieldId + " does not exist in template '" + template.Name + "'", item.Template.InnerItem.Paths.FullPath, item.Paths.FullPath, item.ID);
#else
					throw new Exception("Field " + field.FieldId + " does not exist in template '" + template.Name + "'");
#endif
				}

				_logger.SkippedMissingTemplateField(item, field);
				return;
			}

			Field itemField = item.Fields[ID.Parse(field.FieldId)];
			if (itemField.IsBlobField && !ID.IsID(field.Value))
			{
				byte[] buffer = Convert.FromBase64String(field.Value);
				itemField.SetBlobStream(new MemoryStream(buffer, false));

				//if (!creatingNewItem)
					//_logger.WroteBlobStream(item, field);
			}
			else if (!field.Value.Equals(itemField.Value))
			{
				var oldValue = itemField.Value;
				itemField.SetValue(field.Value, true);
				if (!creatingNewItem)
					_logger.UpdatedChangedFieldValue(item, field, oldValue);
			}
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

			if(template == null)
				throw new DeserializationException("Template {0} for item {1} not found".FormatWith(templateId, itemPath));

			return template;
		}
	}
}
