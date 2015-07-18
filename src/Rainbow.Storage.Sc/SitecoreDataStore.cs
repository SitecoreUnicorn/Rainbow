using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
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

		public void MoveOrRenameItem(IItemData itemWithFinalPath, string oldPath)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(path, "path");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(path);

			if (dbItem == null) yield break;

			yield return new ItemData(dbItem, this);
		}

		public IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			Assert.ArgumentNotNull(parentItem, "parentItem");

			var db = GetDatabase(parentItem.DatabaseName);

			Assert.IsNotNull(db, "Database of item was null! Security issue?");

			var item = db.GetItem(new ID(parentItem.Id));

			return item.Children.Select(child => (IItemData)new ItemData(child, this)).ToArray();
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

		public bool Remove(IItemData item)
		{
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

		protected virtual Database GetDatabase(string databaseName)
		{
			return Factory.GetDatabase(databaseName);
		}
	}
}
