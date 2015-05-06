using System;
using System.Collections.Generic;
using System.Linq;
using Alphaleonis.Win32.Filesystem;
using Gibson.Data;
using Gibson.Indexing;
using Gibson.Model;
using Sitecore.Diagnostics;

namespace Gibson.Storage
{
	public class FileSystemDataStore
	{
		private readonly string _rootPath;
		private readonly PathProvider _pathProvider;
		private readonly ISerializationFormatter _formatter;
		private readonly IIndexFormatter _indexFormatter;
		private readonly IIndex _index;

		public FileSystemDataStore(string rootPath, PathProvider pathProvider, ISerializationFormatter formatter, IIndexFormatter indexFormatter, IIndex index)
		{
			Assert.ArgumentCondition(Directory.Exists(rootPath), "rootPath", "Root path must be a valid directory!");
			Assert.ArgumentNotNull(pathProvider, "pathProvider");
			Assert.ArgumentNotNull(formatter, "formatter");
			Assert.ArgumentNotNull(indexFormatter, "indexFormatter");
			Assert.ArgumentNotNull(index, "index");

			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_formatter = formatter;
			_indexFormatter = indexFormatter;
			_index = index;

			// TODO: method to read index file, run through formatter, and init the index
		}



		/// <summary>
		/// Saves an item into the store
		/// </summary>
		public virtual void Save(ISerializableItem item)
		{
			var path = _pathProvider.GetStoragePath(item.Id, _rootPath);

			// todo: begin transaction

			using (var writer = File.OpenWrite(path))
			{
				_formatter.WriteSerializedItem(item, writer);
			}

			_index.Update(GetIndexEntry(item));
			// todo commit transaction
		}

		protected IndexEntry GetIndexEntry(ISerializableItem item)
		{
			var entry = new IndexEntry();
			entry.LoadFrom(item);
			return entry;
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
