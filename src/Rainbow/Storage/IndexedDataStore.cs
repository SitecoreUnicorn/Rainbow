using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Indexing;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Storage
{
	public abstract class IndexedDataStore : IDataStore
	{
		private readonly IIndexFactory _indexFactory;
		private readonly Dictionary<string, IIndex> _indices = new Dictionary<string, IIndex>(); 

		protected IndexedDataStore(IIndexFactory indexFactory)
		{
			Assert.ArgumentNotNull(indexFactory, "index");

			_indexFactory = indexFactory;
		}

		public abstract IEnumerable<string> GetDatabaseNames();

		/// <summary>
		/// Saves an item into the store
		/// </summary>
		public abstract void Save(ISerializableItem item);

		/// <summary>
		/// Loads an item from the store by ID
		/// </summary>
		/// <returns>The stored item, or null if it does not exist in the store</returns>
		public virtual ISerializableItem GetById(Guid itemId, string database)
		{
			var itemById = GetIndexForDatabase(database).GetById(itemId);

			if (itemById == null) return null;

			return Load(itemById, database, false);
		}

		public virtual IEnumerable<ISerializableItem> GetByPath(string path, string database)
		{
			var itemsOnPath = GetIndexForDatabase(database).GetByPath(path);

			return itemsOnPath.Select(x => Load(x, database, true));
		}

		public virtual IEnumerable<ISerializableItem> GetByTemplate(Guid templateId, string database)
		{
			var itemsOfTemplate = GetIndexForDatabase(database).GetByTemplate(templateId);

			return itemsOfTemplate.Select(x => Load(x, database, true));
		}

		public virtual IEnumerable<ISerializableItem> GetChildren(Guid parentId, string database)
		{
			var childItems = GetIndexForDatabase(database).GetChildren(parentId);

			return childItems.Select(x => Load(x, database, true));
		}

		public virtual IEnumerable<ISerializableItem> GetDescendants(Guid parentId, string database)
		{
			var descendants = GetIndexForDatabase(database).GetDescendants(parentId);

			return descendants.Select(x => Load(x, database, true));
		}

		/// <summary>
		/// Loads all items in the data store
		/// </summary>
		public abstract void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver);

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		public abstract bool Remove(Guid itemId, string database);

		protected abstract ISerializableItem Load(IndexEntry indexData, string database, bool assertExists);

		protected virtual IIndex GetIndexForDatabase(string database)
		{
			IIndex result;
			if (_indices.TryGetValue(database, out result)) return result;
			
			lock (_indices)
			{
				if (_indices.TryGetValue(database, out result)) return result;

				var index = _indexFactory.CreateIndex(database);
				_indices.Add(database, index);

				return index;
			}
		}
	}
}
