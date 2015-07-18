using System;
using System.Collections.Generic;

namespace Rainbow.Model
{
	public interface IItemData
	{
		Guid Id { get; }
		string DatabaseName { get; set; }
		Guid ParentId { get; }
		string Path { get; }
		string Name { get; }
		Guid BranchId { get; }
		Guid TemplateId { get; }
		IEnumerable<IItemFieldValue> SharedFields { get; }
		IEnumerable<IItemVersion> Versions { get; }

		/// <summary>
		/// Provider-specific identifier for a serialized item (e.g. path on disk, row identifier, etc)
		/// </summary>
		string SerializedItemId { get; }

		IEnumerable<IItemData> GetChildren();
	}
}
