using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Alphaleonis.Win32.Filesystem;
using Gibson.Data;
using Gibson.Indexing;
using Gibson.Model;
using Gibson.Storage;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Gibson
{
	public class TransactionalFileSystemSerializationStore : SerializationStore
	{
		private readonly string _rootPath;
		private readonly PathProvider _pathProvider;
		private readonly ISerializationFormatter _formatter;
		private readonly IIndexFormatter _indexFormatter;
		private readonly IIndex _index;
		protected object UpdateLock = new object();

		public TransactionalFileSystemSerializationStore(string rootPath, PathProvider pathProvider, ISerializationFormatter formatter, IIndexFormatter indexFormatter, IIndex index) : base(index)
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

			if (File.Exists(IndexPath))
			{
				ReadOnlyCollection<IndexEntry> entries;
				using (var indexStream = File.OpenRead(IndexPath))
				{
					entries = _indexFormatter.ReadIndex(indexStream);
				}

				_index.Initialize(entries);
			}
			else
			{
				_index.Initialize(new IndexEntry[0]);
			}
		}

		protected virtual string IndexPath
		{
			get { return Path.Combine(_rootPath, "index.gib"); }
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
						var path = _pathProvider.GetStoragePath(item.Id, _rootPath);

						using (var writer = File.OpenWrite(transaction, path))
						{
							_formatter.WriteSerializedItem(item, writer);
						}

						_index.Update(GetIndexEntry(item));

						WriteIndexFile(transaction);
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
		public override void CheckConsistency(bool fixErrors, Action<string> logMessageReceiver)
		{
			var items = _pathProvider.GetAllStoredPaths(_rootPath);

			var indexItems = _index.GetAll();

			// TODO: find items in either files or index but not both
			// TODO: for index orphans, remove them from the index if "fix" enabled
			// TODO: for filesystem orphans, remove from disk if "fix" enabled
		}

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		public override bool Remove(Guid itemId)
		{
			lock (UpdateLock)
			{
				using (var transaction = new KernelTransaction())
				{
					try
					{
						var path = _pathProvider.GetStoragePath(itemId, _rootPath);

						if (path == null || !File.Exists(transaction, path)) return false;
						if (!_index.Remove(itemId)) return false;

						File.Delete(transaction, path);

						WriteIndexFile(transaction);
					}
					catch
					{
						transaction.Rollback();
						throw;
					}

					transaction.Commit();
				}

				return true;
			}
		}

		/// <summary>
		/// NOTE: it's your job to make sure this is also in a critical section.
		/// </summary>
		/// <param name="transaction"></param>
		protected virtual void WriteIndexFile(KernelTransaction transaction)
		{
			using (var indexStream = File.OpenWrite(transaction, IndexPath))
			{
				_indexFormatter.WriteIndex(_index.GetAll(), indexStream);
			}
		}

		protected IndexEntry GetIndexEntry(ISerializableItem item)
		{
			var entry = new IndexEntry();
			entry.LoadFrom(item);
			return entry;
		}

		protected override ISerializableItem Load(Guid itemId, bool assertExists)
		{
			var path = _pathProvider.GetStoragePath(itemId, _rootPath);

			if (path == null || !File.Exists(path))
			{
				if (!assertExists) return null;

				throw new DataConsistencyException("The item {0} was present in the index but no file existed for it on disk. This indicates corruption in the index or data store. Run fsck.".FormatWith(itemId));
			}

			return Load(path);
		}

		protected virtual ISerializableItem Load(string path)
		{
			using (var reader = File.OpenRead(path))
			{
				ISerializableItem item = _formatter.ReadSerializedItem(reader);
				var indexItem = _index.GetById(item.Id);

				if (indexItem == null) throw new DataConsistencyException("The item data at {0} was not present in the index. This indicates corruption in the index or data store. Run fsck.".FormatWith(path));

				item.AddIndexData(indexItem);

				return item;
			}
		}
	}
}
