using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;

namespace Gibson.Indexing
{
	public class IndexStore
	{
		private readonly Dictionary<Guid, IndexEntry> _indexById;
		private readonly Dictionary<Guid, List<IndexEntry>> _indexByTemplate;
		private readonly Dictionary<Guid, List<IndexEntry>> _indexByChildren;
		private readonly Dictionary<string, List<IndexEntry>> _indexByPath;

		public IndexStore(IList<IndexEntry> entries)
		{
			_indexById = new Dictionary<Guid, IndexEntry>(entries.Count);
			_indexByTemplate = new Dictionary<Guid, List<IndexEntry>>(200);
			_indexByPath = new Dictionary<string, List<IndexEntry>>(entries.Count, StringComparer.OrdinalIgnoreCase);
			_indexByChildren = new Dictionary<Guid, List<IndexEntry>>(entries.Count);

			for (var i = 0; i < entries.Count; i++)
			{
				AddEntryToIndices(entries[i]);
			}
		}

		public virtual IndexEntry GetById(Guid id)
		{
			IndexEntry result;
			if(_indexById.TryGetValue(id, out result)) return result;

			return null;
		}

		public virtual IReadOnlyCollection<IndexEntry> GetByPath(string path)
		{
			List<IndexEntry> result;
			if (_indexByPath.TryGetValue(path, out result)) return result.AsReadOnly();

			return new List<IndexEntry>().AsReadOnly();
		}

		public virtual IReadOnlyCollection<IndexEntry> GetByTemplate(Guid templateId)
		{
			List<IndexEntry> result;
			if (_indexByTemplate.TryGetValue(templateId, out result)) return result.AsReadOnly();

			return new List<IndexEntry>().AsReadOnly();
		}

		public virtual IReadOnlyCollection<IndexEntry> GetChildren(Guid parentId)
		{
			List<IndexEntry> result;
			if (_indexByChildren.TryGetValue(parentId, out result)) return result.AsReadOnly();

			return new List<IndexEntry>().AsReadOnly();
		}

		public virtual IEnumerable<IndexEntry> GetDescendants(Guid parentId)
		{
			var queue = new Queue<IndexEntry>(GetChildren(parentId));

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();

				yield return current;

				var children = GetChildren(current.Id);

				foreach (var child in children)
				{
					queue.Enqueue(child);
				}
			}
		} 

		public virtual IReadOnlyCollection<IndexEntry> GetAll()
		{
			return _indexById.Values.ToList().AsReadOnly();
		} 

		public virtual void Update(IndexEntry entry)
		{
			bool dirty;
			var existing = GetById(entry.Id);

			// brand new entry
			if (existing == null)
			{
				AddEntryToIndices(entry);
				dirty = true;
			}
			else if (existing.Path.Equals(entry.Path))
			{
				// no path changed means just a data update to an existing entry
				// if no index data changes, we don't mark it as dirty (saving us a write to disk later)
				dirty = MergeEntry(entry, existing);
			}
			else
			{
				// rename or move - path changed. We need to merge and rewrite descendant paths.
				Assert.AreEqual(existing.ParentPath, entry.ParentPath, "The items' parent IDs matched, but their parent paths did not. This is corrupt data.");

				MoveDescendants(entry, existing);
				MergeEntry(entry, existing);

				dirty = true;
			}

			// note that dirty is only marked if the index has actually changed some values
			if(dirty)
				IsDirty = true;
		}

		/// <summary>
		/// True if the index has been changed since loading.
		/// You may set this to false manually, e.g. after flushing the index to storage successfully.
		/// </summary>
		public virtual bool IsDirty { get; set; }

		protected virtual void MoveDescendants(IndexEntry updatedEntry, IndexEntry existingEntry)
		{
			if(updatedEntry.Id != existingEntry.Id) throw new ArgumentException("Item IDs did not match on old and new. Go away.");

			var descendants = GetDescendants(existingEntry.Id);

			foreach (var descendant in descendants)
			{
				var relativePath = descendant.Path.Substring(existingEntry.Path.Length);

				descendant.Path = string.Concat(updatedEntry.Path, "/", relativePath);
			}
		}

		protected bool MergeEntry(IndexEntry newEntry, IndexEntry entryToMergeTo)
		{
			if(newEntry.Id != entryToMergeTo.Id) throw new ArgumentException("Item IDs to merge did not match. Go away.");

			bool changed = false;

			if (entryToMergeTo.ParentId != newEntry.ParentId)
			{
				entryToMergeTo.ParentId = newEntry.ParentId;
				changed = true;
			}

			if (!entryToMergeTo.Path.Equals(newEntry.Path))
			{
				entryToMergeTo.Path = newEntry.Path;
				changed = true;
			}

			if (entryToMergeTo.TemplateId != newEntry.TemplateId)
			{
				entryToMergeTo.TemplateId = newEntry.TemplateId;
				changed = true;
			}

			return changed;
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
