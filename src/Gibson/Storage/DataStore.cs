using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibson.Formatting;
using Gibson.Model;
using Sitecore.Diagnostics;

namespace Gibson.Storage
{
	public class DataStore
	{
		private readonly string _rootPath;
		private readonly PathProvider _pathProvider;
		private readonly ISerializationFormatter _formatter;
		public DataStore(string rootPath, PathProvider pathProvider, ISerializationFormatter formatter)
		{
			Assert.ArgumentCondition(Directory.Exists(rootPath), "rootPath", "Root path must be a valid directory!");
			Assert.ArgumentNotNull(pathProvider, "pathProvider");
			Assert.ArgumentNotNull(formatter, "formatter");

			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_formatter = formatter;
		}

		/// <summary>
		/// Saves an item into the store
		/// </summary>
		public virtual void Save(ISerializableItem item)
		{
			var path = _pathProvider.GetStoragePath(item.Id, _rootPath);

			using (var writer = File.OpenWrite(path))
			{
				_formatter.WriteSerializedItem(item, writer);
			}
		}

		/// <summary>
		/// Loads an item from the store by ID
		/// </summary>
		/// <returns>The stored item, or null if it does not exist in the store</returns>
		public virtual ISerializableItem Load(Guid itemId)
		{
			var path = _pathProvider.GetStoragePath(itemId, _rootPath);

			if (path == null || !File.Exists(path)) return null;

			return Load(path);
		}

		/// <summary>
		/// Loads all items in the data store (used for consistency checks)
		/// </summary>
		public virtual IEnumerable<ISerializableItem> LoadAll()
		{
			var items = _pathProvider.GetAllStoredPaths(_rootPath);

			if (items == null) return Enumerable.Empty<ISerializableItem>();

			return items.Select(Load);
		}

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		public virtual bool Remove(Guid itemId)
		{
			var path = _pathProvider.GetStoragePath(itemId, _rootPath);

			if (path == null || !File.Exists(path)) return false;

			File.Delete(path);

			return true;
		}

		protected virtual ISerializableItem Load(string path)
		{
			using (var reader = File.OpenRead(path))
			{
				return _formatter.ReadSerializedItem(reader);
			}
		}
	}
}
