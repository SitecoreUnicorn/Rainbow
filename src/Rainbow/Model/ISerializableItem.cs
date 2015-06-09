using System;
using System.Collections.Generic;
using Rainbow.Indexing;

namespace Rainbow.Model
{
	public interface ISerializableItem
	{
		Guid Id { get; }
		string DatabaseName { get; set; }
		Guid ParentId { get; }
		string Path { get; }
		string Name { get; }
		Guid BranchId { get; }
		Guid TemplateId { get; }
		IEnumerable<ISerializableFieldValue> SharedFields { get; }
		IEnumerable<ISerializableVersion> Versions { get; }

		/// <summary>
		/// Provider-specific identifier for a serialized item (e.g. path on disk, row identifier, etc)
		/// </summary>
		string SerializedItemId { get; }

		void AddIndexData(IndexEntry indexEntry);
	}
}
