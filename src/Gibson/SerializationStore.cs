using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Alphaleonis.Win32.Filesystem;
using Gibson.Data;
using Gibson.Indexing;
using Gibson.Model;
using Gibson.Storage;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Gibson
{
	public abstract class SerializationStore
	{
		private readonly IIndex _index;

		protected SerializationStore(IIndex index)
		{
			Assert.ArgumentNotNull(index, "index");

			_index = index;
		}


		/// <summary>
		/// Saves an item into the store
		/// </summary>
		public abstract void Save(ISerializableItem item);

		/// <summary>
		/// Loads an item from the store by ID
		/// </summary>
		/// <returns>The stored item, or null if it does not exist in the store</returns>
		public virtual ISerializableItem GetById(Guid itemId)
		{
			return Load(itemId, false);
		}

		public virtual IEnumerable<ISerializableItem> GetByPath(string path)
		{
			var itemsOnPath = _index.GetByPath(path);

			return itemsOnPath.Select(x => Load(x.Id, true));
		}

		public virtual IEnumerable<ISerializableItem> GetByTemplate(Guid templateId)
		{
			var itemsOfTemplate = _index.GetByTemplate(templateId);

			return itemsOfTemplate.Select(x => Load(x.Id, true));
		}

		public virtual IEnumerable<ISerializableItem> GetChildren(Guid parentId)
		{
			var childItems = _index.GetChildren(parentId);

			return childItems.Select(x => Load(x.Id, true));
		}

		public virtual IEnumerable<ISerializableItem> GetDescendants(Guid parentId)
		{
			var descendants = _index.GetDescendants(parentId);

			return descendants.Select(x => Load(x.Id, true));
		}

		/// <summary>
		/// Loads all items in the data store
		/// </summary>
		public abstract void CheckConsistency(bool fixErrors, Action<string> logMessageReceiver);

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		public abstract bool Remove(Guid itemId);

		protected abstract ISerializableItem Load(Guid itemId, bool assertExists);
	}
}
