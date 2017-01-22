using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;

namespace Rainbow.Storage.Sc
{
	public class SitecoreDataStore : IDataStore, IDocumentable
	{
		private readonly IDeserializer _deserializer;

		public SitecoreDataStore(IDeserializer deserializer)
		{
			Assert.ArgumentNotNull(deserializer, "deserializer");

			_deserializer = deserializer;
			_deserializer.ParentDataStore = this;
		}

		public virtual IEnumerable<string> GetDatabaseNames()
		{
			return Factory.GetDatabaseNames();
		}

		public virtual void Save(IItemData item)
		{
			Assert.ArgumentNotNull(item, "item");

			_deserializer.Deserialize(item);
		}

		public virtual void MoveOrRenameItem(IItemData itemWithFinalPath, string oldPath)
		{
			// We don't ask the Sitecore provider to move or rename
			throw new NotImplementedException();
		}

		public virtual IEnumerable<IItemData> GetByPath(string path, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(path, "path");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			// note: this is awfully slow. But the only way to get items by path that finds ALL matches of the path instead of the first.
			// luckily most queries will use GetByMetadata with the ID, which is fast
			var dbItems = db.SelectItems(path);

			if (dbItems == null || dbItems.Length == 0) return Enumerable.Empty<IItemData>();

			return dbItems.Select(item => new ItemData(item, this));
		}

		public virtual IItemData GetByPathAndId(string path, Guid id, string database)
		{
			// note: because we always have the ID this is just GetById() for Sitecore
			return GetById(id, database);
		}

		public virtual IItemData GetById(Guid id, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentCondition(id != default(Guid), "id", "The item ID must not be the null guid. Use GetByPath() if you don't know the ID.");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var item = db.GetItem(new ID(id));
			return item == null ? null : new ItemData(item);
		}

		public virtual IEnumerable<IItemMetadata> GetMetadataByTemplateId(Guid templateId, string database)
		{
			throw new NotImplementedException();
		}

		public virtual IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			Assert.ArgumentNotNull(parentItem, "parentItem");

			var db = GetDatabase(parentItem.DatabaseName);

			Assert.IsNotNull(db, "Database of item was null! Security issue?");

			var item = db.GetItem(new ID(parentItem.Id));

			if (item == null) return Enumerable.Empty<IItemData>();

			return item.GetChildren(ChildListOptions.SkipSorting).Select(child => (IItemData)new ItemData(child, this)).ToArray();
		}

		public virtual void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			// do nothing - the Sitecore database is always considered consistent.
		}

		public virtual void ResetTemplateEngine()
		{
			foreach (Database current in Factory.GetDatabases())
			{
				current.Engines.TemplateEngine.Reset();
			}
		}

		public virtual bool Remove(IItemData item)
		{
			Assert.ArgumentNotNull(item, "item");

			var databaseRef = GetDatabase(item.DatabaseName);
			var scId = new ID(item.Id);
			var scItem = databaseRef.GetItem(scId);

			if (scItem == null) return false;

			scItem.Recycle();

			if (EventDisabler.IsActive)
			{
				databaseRef.Caches.ItemCache.RemoveItem(scId);
				databaseRef.Caches.DataCache.RemoveItemInformation(scId);
			}

			if (databaseRef.Engines.TemplateEngine.IsTemplatePart(scItem))
			{
				databaseRef.Engines.TemplateEngine.Reset();
			}

			return true;
		}

		public virtual void RegisterForChanges(Action<IItemMetadata, string> actionOnChange)
		{
			throw new NotImplementedException("You may not watch Sitecore for changes.");
		}

		public virtual void Clear()
		{
			throw new NotImplementedException("You crazy? I'm not going to clear the Sitecore database for you! :)");
		}

		protected virtual Database GetDatabase(string databaseName)
		{
			return Factory.GetDatabase(databaseName);
		}

		public virtual string FriendlyName => "Sitecore Data Store";
		public virtual string Description => "Reads and writes data from a Sitecore database.";

		public virtual KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new [] { new KeyValuePair<string, string>("Deserializer", DocumentationUtility.GetFriendlyName(_deserializer)) };
		}
	}
}
