using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
		private readonly FileSystemWatcher _watcher;

		public YamlSerializationDataStore(string rootPath, bool watchForChanges, IFileSystemPathProvider pathProvider, ISerializationFormatter formatter)
			: base(new YamlFrontMatterIndexFactory(rootPath, pathProvider))
		{
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");
			Assert.ArgumentNotNull(pathProvider, "pathProvider");
			Assert.ArgumentNotNull(formatter, "formatter");

			_rootPath = rootPath;
			_pathProvider = pathProvider;
			_formatter = (YamlSerializationFormatter)formatter;

			_pathProvider.FileExtension = ".yml";

			if (watchForChanges)
			{
				_watcher = new FileSystemWatcher(rootPath, "*") { IncludeSubdirectories = true };
				_watcher.Changed += OnFileChanged;
				_watcher.Created += OnFileChanged;
				_watcher.Deleted += OnFileChanged;
				_watcher.EnableRaisingEvents = true;
			}
		}

		public override IEnumerable<string> GetDatabaseNames()
		{
			return _pathProvider.GetAllStoredDatabaseNames(_rootPath);
		}

		public override void Save(IItemData item)
		{
			var path = _pathProvider.GetStoragePath(new IndexEntry().LoadFrom(item), item.DatabaseName, _rootPath);

			Directory.CreateDirectory(Path.GetDirectoryName(path));

			using (var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
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

		protected override IItemData Load(IndexEntry indexData, string database, bool assertExists)
		{
			var path = _pathProvider.GetStoragePath(indexData, database, _rootPath);

			if (path == null || !File.Exists(path))
			{
				if (!assertExists) return null;

				throw new DataConsistencyException("The item {0} was present in the index but no file existed for it on disk. This indicates corruption in the index or data store. Run fsck.".FormatWith(indexData));
			}

			return Load(path, database);
		}

		protected virtual IItemData Load(string path, string database)
		{
			using (var reader = File.OpenRead(path))
			{
				// no need for the index here because front matter formatters will inject it
				var result = _formatter.ReadSerializedItem(reader, path);
				result.DatabaseName = database;

				return result;
			}
		}

		protected virtual void OnFileChanged(object source, FileSystemEventArgs args)
		{
			var changeType = args.ChangeType;

			if (!File.Exists(args.FullPath)) return;

			if (changeType == WatcherChangeTypes.Created || changeType == WatcherChangeTypes.Changed)
			{
				Log.Info(string.Format("[Rainbow] Serialized item {0} changed ({1}), updating index.", args.FullPath, changeType), this);

				const int retries = 4;
				for (int i = 0; i < retries; i++)
				{
					try
					{
						using (var stream = File.OpenRead(args.FullPath))
						{
							var yamlItem = _formatter.ReadSerializedItem(stream, args.FullPath);

							var indexEntry = new IndexEntry
							{
								Id = yamlItem.Id,
								ParentId = yamlItem.ParentId,
								Path = yamlItem.Path,
								TemplateId = yamlItem.TemplateId
							};

							var databaseName = _pathProvider.GetDatabaseNameFromPath(args.FullPath, _rootPath);

							GetIndexForDatabase(databaseName).Update(indexEntry);
						}
					}
					catch (IOException iex)
					{
						// this is here because FSW can tell us the file has changed
						// BEFORE it's done with writing. So if we get access denied,
						// we wait 500ms and retry up to 4x before rethrowing
						if (i < retries - 1)
						{
							Thread.Sleep(500);
							continue;
						}

						Log.Error("[Rainbow] Failed to read serialization file " + args.FullPath, iex, this);
					}
					catch (Exception ex)
					{
						Log.Warn("[Rainbow] Unable to parse changed item {0}; will retry if changed again.".FormatWith(args.FullPath), ex, this);
					}

					break;
				}
			}

			if (changeType == WatcherChangeTypes.Deleted)
			{
				Log.Info(string.Format("[Rainbow] Serialized item {0} deleted, updating index.", args.FullPath), this);

				var databaseName = _pathProvider.GetDatabaseNameFromPath(args.FullPath, _rootPath);

				var index = GetIndexForDatabase(databaseName);

				var indexEntry = _pathProvider.FindItemByPhysicalPath(args.FullPath, _rootPath, index);

				if (indexEntry != null) index.Remove(indexEntry.Id);
			}
		}

		~YamlSerializationDataStore()
		{
			if(_watcher != null) _watcher.Dispose();
		}
	}
}
