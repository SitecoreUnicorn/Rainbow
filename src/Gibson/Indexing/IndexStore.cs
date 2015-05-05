using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Gibson.Formatting;
using Sitecore.Diagnostics;

namespace Gibson.Indexing
{
	public class IndexStore
	{
		private readonly string _filePath;
		private readonly IIndexFormatter _formatter;

		private readonly object _syncLock = new object();

		private Dictionary<Guid, IndexEntry> _indexById;
		private Dictionary<Guid, List<IndexEntry>> _indexByTemplate;
		private Dictionary<Guid, List<IndexEntry>> _indexByChildren;
		private Dictionary<string, List<IndexEntry>> _indexByPath;

		private bool _isDirty;
		private bool _isInitialized = false;

		public IndexStore(string filePath, IIndexFormatter formatter)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
			Assert.IsTrue(File.Exists(filePath), "Index file did not exist on disk.");
			Assert.ArgumentNotNull(formatter, "formatter");
			
			_filePath = filePath;
			_formatter = formatter;

			// todo: filesystem watcher open on the index file? (need good disposal/finalization)
		}

		public virtual IndexEntry GetById(Guid id)
		{
			if(!_isInitialized) ReadIndexFile(false);

			IndexEntry result;
			if(_indexById.TryGetValue(id, out result)) return result;

			return null;
		}

		public virtual IReadOnlyCollection<IndexEntry> GetByPath(string path)
		{
			if (!_isInitialized) ReadIndexFile(false);

			List<IndexEntry> result;
			if (_indexByPath.TryGetValue(path, out result)) return result.AsReadOnly();

			return new List<IndexEntry>().AsReadOnly();
		}

		public virtual IReadOnlyCollection<IndexEntry> GetByTemplate(Guid templateId)
		{
			if (!_isInitialized) ReadIndexFile(false);

			List<IndexEntry> result;
			if (_indexByTemplate.TryGetValue(templateId, out result)) return result.AsReadOnly();

			return new List<IndexEntry>().AsReadOnly();
		}

		public virtual IReadOnlyCollection<IndexEntry> GetChildren(Guid parentId)
		{
			if (!_isInitialized) ReadIndexFile(false);

			List<IndexEntry> result;
			if (_indexByChildren.TryGetValue(parentId, out result)) return result.AsReadOnly();

			return new List<IndexEntry>().AsReadOnly();
		}

		public virtual IReadOnlyCollection<IndexEntry> GetAll()
		{
			if (!_isInitialized) ReadIndexFile(false);

			return _indexById.Values.ToList().AsReadOnly();
		} 

		public virtual void Update(IndexEntry entry, bool commit)
		{
			_isDirty = true;
			AddEntryToIndices(entry); // naive, needs fixing. this method probably needs to be multiple because we don't know what to do with it
			// TODO: moves? renames? w/path and children, and what about jagged children (partially serialized)
			if(commit) UpdateIndexFile();
		}

		public virtual void Commit()
		{
			if(_isDirty) UpdateIndexFile();
		}

		protected virtual void ReadIndexFile(bool force)
		{
			if (!force && _isInitialized) return;

			lock (_syncLock)
			{
				if (!force && _isInitialized) return;

				_isInitialized = false;

				ReadOnlyCollection<IndexEntry> entries;
				using (var reader = File.OpenRead(_filePath))
				{
					entries = _formatter.ReadIndex(reader);
				}

				_indexById = new Dictionary<Guid, IndexEntry>(entries.Count);
				_indexByTemplate = new Dictionary<Guid, List<IndexEntry>>(200);
				_indexByPath = new Dictionary<string, List<IndexEntry>>(entries.Count, StringComparer.OrdinalIgnoreCase);
				_indexByChildren = new Dictionary<Guid, List<IndexEntry>>(entries.Count);

				for (var i = 0; i < entries.Count; i++)
				{
					AddEntryToIndices(entries[i]);
				}

				_isInitialized = true;
			}
		}

		protected virtual void UpdateIndexFile()
		{
			lock (_syncLock)
			{
				using (var writer = File.OpenWrite(_filePath))
				{
					_formatter.WriteIndex(_indexById.Values, writer);
				}
			}
		}

		protected void AddEntryToIndices(IndexEntry entry)
		{
			_indexById.Add(entry.Id, entry);

			var template = EnsureCollectionKey(_indexByTemplate, entry.TemplateId);
			template.Add(entry);

			var path = EnsureCollectionKey(_indexByPath, entry.Path);
			path.Add(entry);

			var children = EnsureCollectionKey(_indexByChildren, entry.ParentId);
			children.Add(entry);
		}

		protected List<IndexEntry> EnsureCollectionKey<T>(Dictionary<T, List<IndexEntry>> dictionary, T key)
		{
			List<IndexEntry> indexEntry;
			if(dictionary.TryGetValue(key, out indexEntry)) return indexEntry;

			indexEntry = new List<IndexEntry>();
			dictionary.Add(key, indexEntry);

			return indexEntry;
		} 
	}
}
