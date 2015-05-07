using System;
using System.Collections.Generic;

namespace Gibson.Indexing
{
	public interface IIndex
	{
		void Initialize(IList<IndexEntry> entries);
		void Update(IndexEntry indexEntry);
		IReadOnlyCollection<IndexEntry> GetAll();
		IndexEntry GetById(Guid itemId);
		IReadOnlyCollection<IndexEntry> GetByPath(string path);
		IReadOnlyCollection<IndexEntry> GetByTemplate(Guid templateId);
		IReadOnlyCollection<IndexEntry> GetChildren(Guid parentId);
		IEnumerable<IndexEntry> GetDescendants(Guid parentId);
		bool Remove(Guid itemId);
	}
}
