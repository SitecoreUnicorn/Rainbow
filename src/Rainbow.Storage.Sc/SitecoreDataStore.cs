using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;

namespace Rainbow.Storage.Sc
{
	public class SitecoreDataStore : IDataStore
	{
		private readonly IDeserializer _deserializer;

		public SitecoreDataStore(IDeserializer deserializer)
		{
			Assert.ArgumentNotNull(deserializer, "deserializer");

			_deserializer = deserializer;
		}

		public IEnumerable<string> GetDatabaseNames()
		{
			return Factory.GetDatabaseNames();
		}

		public void Save(IItemData item)
		{
			_deserializer.Deserialize(item, false);
		}

		public IItemData GetById(Guid itemId, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(new ID(itemId));

			if (dbItem == null) return null;

			return new ItemData(dbItem);
		}

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(path, "path");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(path);

			if (dbItem == null) yield break;

			yield return new ItemData(dbItem);
		}

		public IEnumerable<IItemData> GetByTemplate(Guid templateId, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			
			var db = GetDatabase(database);
			var templateItem = db.GetItem(new ID(templateId));

			if (templateItem == null) return Enumerable.Empty<IItemData>();

			return Globals.LinkDatabase.GetReferrers(templateItem)
				.Where(link => link.SourceDatabaseName.Equals(database, StringComparison.OrdinalIgnoreCase) && link.SourceFieldID == ID.Null)
				.Select(link => link.GetSourceItem())
				.Where(linkItem => linkItem != null && linkItem.TemplateID.Equals(new ID(templateId)))
				.Select(linkItem => new ItemData(linkItem));
		}

		public IEnumerable<IItemData> GetChildren(Guid parentId, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");

			var db = GetDatabase(database);

			Assert.IsNotNull(db, "Database of item was null! Security issue?");

			var item = db.GetItem(new ID(parentId));

			return item.Children.Select(child => (IItemData)new ItemData(child)).ToArray();
		}

		public IEnumerable<IItemData> GetDescendants(Guid parentId, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");

			var db = GetDatabase(database);

			Assert.IsNotNull(db, "Database of item was null! Security issue?");

			return db.GetItem(new ID(parentId)).Axes.GetDescendants()
				.Select(descendant => new ItemData(descendant));
		}

		public void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			// do nothing - the Sitecore database is always considered consistent.
		}

		public void ResetTemplateEngine()
		{
			foreach (Database current in Factory.GetDatabases())
			{
				current.Engines.TemplateEngine.Reset();
			}
		}

		public bool Remove(Guid itemId, string database)
		{
			var databaseRef = GetDatabase(database);
			var scId = new ID(itemId);
			var item = databaseRef.GetItem(scId);

			if (item == null) return false;

			item.Recycle();

			if (EventDisabler.IsActive)
			{
				databaseRef.Caches.ItemCache.RemoveItem(scId);
				databaseRef.Caches.DataCache.RemoveItemInformation(scId);
			}

			if (databaseRef.Engines.TemplateEngine.IsTemplatePart(item))
			{
				databaseRef.Engines.TemplateEngine.Reset();
			}

			return true;
		}

		protected virtual Database GetDatabase(string databaseName)
		{
			return Factory.GetDatabase(databaseName);
		}
	}
}
