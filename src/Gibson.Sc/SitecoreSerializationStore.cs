using System;
using System.Collections.Generic;
using System.Linq;
using Gibson.Model;
using Gibson.Sc.Deserialization;
using Gibson.Storage;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;

namespace Gibson.Sc
{
	public class SitecoreSerializationStore : IDataStore
	{
		private readonly IDeserializer _deserializer;

		public SitecoreSerializationStore(IDeserializer deserializer)
		{
			Assert.ArgumentNotNull(deserializer, "deserializer");

			_deserializer = deserializer;
		}

		public IEnumerable<string> GetDatabaseNames()
		{
			return Factory.GetDatabaseNames();
		}

		public void Save(ISerializableItem item)
		{
			_deserializer.Deserialize(item, false);
		}

		public ISerializableItem GetById(Guid itemId, string database)
		{
			return new SerializableItem(GetDatabase(database).GetItem(new ID(itemId)));
		}

		public IEnumerable<ISerializableItem> GetByPath(string path, string database)
		{
			yield return new SerializableItem(GetDatabase(database).GetItem(path));
		}

		public IEnumerable<ISerializableItem> GetByTemplate(Guid templateId, string database)
		{
			var db = GetDatabase(database);
			var templateItem = db.GetItem(new ID(templateId));

			if (templateItem == null) return Enumerable.Empty<ISerializableItem>();

			return Globals.LinkDatabase.GetReferrers(templateItem)
				.Where(link => link.SourceDatabaseName.Equals(database, StringComparison.OrdinalIgnoreCase) && link.SourceFieldID == ID.Null)
				.Select(link => link.GetSourceItem())
				.Where(linkItem => linkItem != null && linkItem.TemplateID.Equals(new ID(templateId)))
				.Select(linkItem => new SerializableItem(linkItem));
		}

		public IEnumerable<ISerializableItem> GetChildren(Guid parentId, string database)
		{
			return GetDatabase(database).GetItem(new ID(parentId)).Children.Select(child => new SerializableItem(child));
		}

		public IEnumerable<ISerializableItem> GetDescendants(Guid parentId, string database)
		{
			return GetDatabase(database).GetItem(new ID(parentId)).Axes.GetDescendants().Select(descendant => new SerializableItem(descendant));
		}

		public void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			// do nothing - the Sitecore database is always considered consistent.
		}

		public bool Remove(Guid itemId, string database)
		{
			var item = GetDatabase(database).GetItem(new ID(itemId));

			if (item == null) return false;

			item.Recycle();

			return true;
		}

		protected virtual Database GetDatabase(string databaseName)
		{
			return Factory.GetDatabase(databaseName);
		}
	}
}
