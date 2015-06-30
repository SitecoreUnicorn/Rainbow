using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Formatting;
using Rainbow.Indexing;
using Rainbow.Model;
using Rainbow.Storage.Pathing;
using Rainbow.Storage.Yaml.Formatting;
using Rainbow.Storage.Yaml.Indexing;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Rainbow.Storage.Yaml
{
	public class YamlSerializationDataStore : IndexedDataStore
	{
		private readonly string _rootPath;
		private readonly IFileSystemPathProvider _pathProvider;
		private readonly YamlSerializationFormatter _formatter;

		public YamlSerializationDataStore(string rootPath, IFileSystemPathProvider pathProvider, ISerializationFormatter formatter)
			: base(new YamlFrontMatterIndexFactory(rootPath, pathProvider))
		{
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");
			Assert.ArgumentNotNull(pathProvider, "pathProvider");
			Assert.ArgumentNotNull(formatter, "formatter");

			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_formatter = (YamlSerializationFormatter)formatter;
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
			// TODO: consistency check
			throw new NotImplementedException();
		}

		public override void ResetTemplateEngine()
		{
			// do nothing, the YAML serializer has no template engine
		}

		public override bool Remove(Guid itemId, string database)
		{
			var existingItem = GetById(itemId, database);

			if (existingItem == null) return false;

			var descendants = GetDescendants(itemId, database);

			foreach (var item in descendants.Concat(new[] { existingItem }))
			{
				var path = _pathProvider.GetStoragePath(new IndexEntry().LoadFrom(item), database, _rootPath);

				if (path == null || !File.Exists(path)) return false;
				if (!GetIndexForDatabase(database).Remove(item.Id)) return false;

				File.Delete(path);

				// TODO: check if item was a template field item - and if so, add to a queue to force-save all items of that template - sans that field
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
				var result = _formatter.ReadSerializedItem(reader, path);
				result.DatabaseName = database;

				return result;
			}
		}
	}
}
