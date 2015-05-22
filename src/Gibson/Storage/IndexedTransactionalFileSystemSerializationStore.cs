using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Alphaleonis.Win32.Filesystem;
using Gibson.Indexing;
using Gibson.Model;
using Gibson.SerializationFormatting;
using Gibson.Storage.Pathing;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Gibson.Storage
{
	public class IndexedTransactionalFileSystemSerializationStore : IndexedSerializationStore
	{
		private readonly string _rootPath;
		private readonly IFileSystemPathProvider _pathProvider;
		private readonly ISerializationFormatter _formatter;
		private readonly IIndexFormatter _indexFormatter;
		protected object UpdateLock = new object();

		public IndexedTransactionalFileSystemSerializationStore(string rootPath, IFileSystemPathProvider pathProvider, ISerializationFormatter formatter, IIndexFormatter indexFormatter)
			: base(new IndexedFileSystemStoreIndexFactory(indexFormatter, pathProvider, rootPath))
		{
			Assert.ArgumentCondition(Directory.Exists(rootPath), "rootPath", "Root path must be a valid directory!");
			Assert.ArgumentNotNull(pathProvider, "pathProvider");
			Assert.ArgumentNotNull(formatter, "formatter");
			Assert.ArgumentNotNull(indexFormatter, "indexFormatter");

			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_pathProvider.FileExtension = ".json";
			_pathProvider.IndexFileName = "index.gib";

			_formatter = formatter;
			_indexFormatter = indexFormatter;
		}

		protected virtual string IndexPath
		{
			get { return Path.Combine(_rootPath, "index.gib"); }
		}

		public override IEnumerable<string> GetDatabaseNames()
		{
			return _pathProvider.GetAllStoredDatabaseNames(_rootPath);
		}

		/// <summary>
		/// Saves an item into the store
		/// </summary>
		public override void Save(ISerializableItem item)
		{
			lock (UpdateLock)
			{
				using (var transaction = new KernelTransaction())
				{
					try
					{
						var path = _pathProvider.GetStoragePath(new IndexEntry().LoadFrom(item), item.DatabaseName, _rootPath);

						Directory.CreateDirectory(transaction, Path.GetDirectoryName(path));

						using (var writer = File.OpenWrite(transaction, path))
						{
							_formatter.WriteSerializedItem(item, writer);
						}

						GetIndexForDatabase(item.DatabaseName).Update(GetIndexEntry(item));

						WriteIndexFile(item.DatabaseName, transaction);
					}
					catch
					{
						transaction.Rollback();
						throw;
					}

					transaction.Commit();
				}
			}
		}

		/// <summary>
		/// Loads all items in the data store
		/// </summary>
		public override void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			var items = _pathProvider.GetAllStoredPaths(_rootPath, database);

			var indexItems = GetIndexForDatabase(database).GetAll();

			// TODO: find items in either files or index but not both
			// TODO: for index orphans, remove them from the index if "fix" enabled
			// TODO: for filesystem orphans, remove from disk if "fix" enabled
			var orphans = _pathProvider.GetOrphans(_rootPath, database);
		}

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		public override bool Remove(Guid itemId, string database)
		{
			var existingItem = GetById(itemId, database);

			if (existingItem == null) return false;

			var descendants = GetDescendants(itemId, database);

			lock (UpdateLock)
			{
				using (var transaction = new KernelTransaction())
				{
					try
					{
						foreach (var item in descendants.Concat(new[] { existingItem }))
						{
							var path = _pathProvider.GetStoragePath(new IndexEntry().LoadFrom(item), database, _rootPath);

							if (path == null || !File.Exists(transaction, path)) return false;
							if (!GetIndexForDatabase(database).Remove(item.Id)) return false;

							File.Delete(transaction, path);
						}
						
						WriteIndexFile(database, transaction);
					}
					catch
					{
						transaction.Rollback();
						throw;
					}

					transaction.Commit();
				}
			}

			return true;
		}

		/// <summary>
		/// NOTE: it's your job to make sure this is also in a critical section.
		/// </summary>
		protected virtual void WriteIndexFile(string database, KernelTransaction transaction)
		{
			var dbIndexPath = _pathProvider.GetIndexStoragePath(database, _rootPath);
			var index = GetIndexForDatabase(database);

			using (var indexStream = File.OpenWrite(transaction, dbIndexPath))
			{
				_indexFormatter.WriteIndex(index.GetAll(), indexStream);
			}
		}

		protected IndexEntry GetIndexEntry(ISerializableItem item)
		{
			return new IndexEntry().LoadFrom(item);
		}

		protected override ISerializableItem Load(IndexEntry indexData, string database, bool assertExists)
		{
			var path = _pathProvider.GetStoragePath(indexData, database, _rootPath);

			if (path == null || !File.Exists(path))
			{
				if (!assertExists) return null;

				throw new DataConsistencyException("The item {0} was present in the index but no file existed for it on disk. This indicates corruption in the index or data store. Run fsck.".FormatWith(indexData));
			}

			return Load(path, database);
		}

		protected virtual ISerializableItem Load(string path, string database)
		{
			using (var reader = File.OpenRead(path))
			{
				ISerializableItem item = _formatter.ReadSerializedItem(reader, path);
				var indexItem = GetIndexForDatabase(database).GetById(item.Id);

				if (indexItem == null) throw new DataConsistencyException("The item data at {0} was not present in the index. This indicates corruption in the index or data store. Run fsck.".FormatWith(path));

				item.AddIndexData(indexItem);

				return item;
			}
		}

		protected class IndexedFileSystemStoreIndexFactory : IIndexFactory
		{
			private readonly IIndexFormatter _indexFormatter;
			private readonly IFileSystemPathProvider _pathProvider;
			private readonly string _rootPath;

			public IndexedFileSystemStoreIndexFactory(IIndexFormatter indexFormatter, IFileSystemPathProvider pathProvider, string rootPath)
			{
				_indexFormatter = indexFormatter;
				_pathProvider = pathProvider;
				_rootPath = rootPath;
			}

			public IIndex CreateIndex(string databaseName)
			{
				var indexPath = _pathProvider.GetIndexStoragePath(databaseName, _rootPath);

				if (File.Exists(indexPath))
				{
					ReadOnlyCollection<IndexEntry> entries;
					using (var indexStream = File.OpenRead(indexPath))
					{
						entries = _indexFormatter.ReadIndex(indexStream);
					}

					return new Index(entries);
				}

				return new Index(new IndexEntry[0]);
			}
		}
	}
}
