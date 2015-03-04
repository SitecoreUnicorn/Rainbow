using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibson.IO;
using Sitecore.Data;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;

namespace Gibson.Storage
{
	public class DataStore
	{
		private readonly string _rootPath;
		private readonly PathProvider _pathProvider;
		private readonly GibReader _reader;
		private readonly GibWriter _writer;

		public DataStore(string rootPath, PathProvider pathProvider, GibReader reader, GibWriter writer)
		{
			Assert.ArgumentCondition(Directory.Exists(rootPath), "rootPath", "Root path must be a valid directory!");
			Assert.ArgumentNotNull(pathProvider, "pathProvider");
			Assert.ArgumentNotNull(reader, "reader");
			Assert.ArgumentNotNull(writer, "writer");

			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_reader = reader;
			_writer = writer;
		}

		/// <summary>
		/// Saves an item into the store
		/// </summary>
		public virtual void Save(SyncItem item)
		{
			var path = _pathProvider.GetStoragePath(ID.Parse(item.ID), _rootPath);

			// TODO: lock this (stringlock?)
			using (var writer = File.OpenWrite(path))
			{
				using (var textWriter = new StreamWriter(writer))
				{
					_writer.WriteGib(item, textWriter);
				}
			}
		}

		/// <summary>
		/// Loads an item from the store by ID
		/// </summary>
		/// <returns>The stored item, or null if it does not exist in the store</returns>
		public virtual SyncItem Load(ID itemId)
		{
			var path = _pathProvider.GetStoragePath(itemId, _rootPath);

			if (path == null || !File.Exists(path)) return null;

			return Load(path);
		}

		/// <summary>
		/// Loads all items in the data store (used for consistency checks)
		/// </summary>
		public virtual IEnumerable<SyncItem> LoadAll()
		{
			var items = _pathProvider.GetAllStoredPaths(_rootPath);

			if (items == null) return Enumerable.Empty<SyncItem>();

			return items.Select(Load);
		}

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		public virtual bool Remove(ID itemId)
		{
			var path = _pathProvider.GetStoragePath(itemId, _rootPath);

			if (path == null || !File.Exists(path)) return false;

			File.Delete(path);

			return true;
		}

		protected virtual SyncItem Load(string path)
		{
			// todo: lock this? stringlock?
			using (var reader = File.OpenRead(path))
			{
				using (var textReader = new StreamReader(reader))
				{
					return _reader.ReadGib(textReader);
				}
			}
		}
	}
}
