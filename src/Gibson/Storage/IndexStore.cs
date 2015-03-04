using System.Collections.Generic;
using Gibson.IO;
using Sitecore.Data;

namespace Gibson.Storage
{
	public class IndexStore
	{
		private bool _isDirty;

		public IndexStore(string filePath, IndexReader reader, IndexWriter writer)
		{
			// todo: filesystem watcher open on the index file? (need good disposal/finalization)
		}

		public virtual IndexEntry GetById(ID id)
		{
			return null;
		}

		public virtual IReadOnlyCollection<IndexEntry> GetByPath(string path)
		{
			return null;
		}

		public virtual IReadOnlyCollection<IndexEntry> GetByTemplate(ID templateId)
		{
			return null;
		}

		public virtual IReadOnlyCollection<IndexEntry> GetChildren(ID parentId)
		{
			return null;
		} 

		public virtual void Update(IndexEntry entry, bool commit)
		{
			_isDirty = true;
			// TODO: moves? renames? w/path and children, and what about jagged children (partially serialized)
			if(commit) UpdateIndexFile();
		}

		public virtual void Commit()
		{
			if(_isDirty) UpdateIndexFile();
		}

		protected virtual void ReadIndexFile()
		{
			// TODO: lock this as threads may compete (stringlock?)
		}

		protected virtual void UpdateIndexFile()
		{
			// TODO: lock this as threads may compete (stringlock?)
		}
	}
}
