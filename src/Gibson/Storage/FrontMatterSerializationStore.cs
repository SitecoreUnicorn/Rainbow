using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Gibson.Indexing;
using Gibson.Model;
using Gibson.SerializationFormatting;
using Gibson.Storage.Pathing;
using Sitecore.StringExtensions;

namespace Gibson.Storage
{
	public class FrontMatterSerializationStore : IndexedSerializationStore
	{
		private readonly string _rootPath;
		private readonly IFileSystemPathProvider _pathProvider;
		private readonly ISerializationFormatter _formatter;

		public FrontMatterSerializationStore(string rootPath, IFileSystemPathProvider pathProvider, ISerializationFormatter formatter)
			: base(new FrontMatterIndexFactory(rootPath, pathProvider))
		{
			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_formatter = formatter;
		}

		public override IEnumerable<string> GetDatabaseNames()
		{
			return _pathProvider.GetAllStoredDatabaseNames(_rootPath);
		}

		public override void Save(ISerializableItem item)
		{
			var path = _pathProvider.GetStoragePath(new IndexEntry().LoadFrom(item), item.DatabaseName, _rootPath);

			Directory.CreateDirectory(Path.GetDirectoryName(path));

			using (var writer = File.OpenWrite(path))
			{
				_formatter.WriteSerializedItem(item, writer);
			}

			GetIndexForDatabase(item.DatabaseName).Update(new IndexEntry().LoadFrom(item));
		}

		public override void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			throw new NotImplementedException();
		}

		public override bool Remove(Guid itemId, string database)
		{
			// TODO: by path would be more effective for this provider. idk if we can swap that? or maybe add ID to the item name on disk or something?

			var existingItem = GetById(itemId, database);

			if (existingItem == null) return false;

			var descendants = GetDescendants(itemId, database);

			foreach (var item in descendants.Concat(new[] { existingItem }))
			{
				var path = _pathProvider.GetStoragePath(new IndexEntry().LoadFrom(item), database, _rootPath);

				if (path == null || !File.Exists(path)) return false;
				if (!GetIndexForDatabase(database).Remove(item.Id)) return false;

				File.Delete(path);
			}

			return true;
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
				// no need for the index here because front matter formatters will inject it
				return _formatter.ReadSerializedItem(reader, path);
			}
		}

		protected class FrontMatterIndexFormatter : IIndexFormatter
		{
			private readonly IFileSystemPathProvider _pathProvider;
			private readonly string _rootPath;
			private readonly string _databaseName;
			// TODO: naughty for prototype
			private readonly FrontMatterIndexJsonSerializationFormatter _formatter = new FrontMatterIndexJsonSerializationFormatter();

			public FrontMatterIndexFormatter(IFileSystemPathProvider pathProvider, string rootPath, string databaseName)
			{
				_pathProvider = pathProvider;
				_rootPath = rootPath;
				_databaseName = databaseName;
			}

			public ReadOnlyCollection<IndexEntry> ReadIndex(Stream sourceStream)
			{
				return _pathProvider.GetAllStoredPaths(_rootPath, _databaseName)
					.AsParallel()
					.Select(x =>
					{
						using (var stream = File.OpenRead(x))
						{
							return _formatter.ReadFrontMatter(stream);
						}
					})
					.ToList()
					.AsReadOnly();
			}

			public void WriteIndex(IEnumerable<IndexEntry> entries, Stream outputStream)
			{
				// a front matter index stores its entries with the data
				throw new NotImplementedException();
			}
		}

		protected class FrontMatterIndexFactory : IIndexFactory
		{
			private readonly string _rootPath;
			private readonly IFileSystemPathProvider _pathProvider;

			public FrontMatterIndexFactory(string rootPath, IFileSystemPathProvider pathProvider)
			{
				_rootPath = rootPath;
				_pathProvider = pathProvider;
			}

			public IIndex CreateIndex(string databaseName)
			{
				var formatter = new FrontMatterIndexFormatter(_pathProvider, _rootPath, databaseName);

				var entries = formatter.ReadIndex(null);

				return new Index(entries);
			}
		}
	}
}
