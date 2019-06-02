using System;
using System.Collections.Generic;
using Rainbow.Storage;

namespace Rainbow.Model
{
	public interface IItemData : IItemMetadata
	{
		string DatabaseName { get; set; }
		string Name { get; }
		Guid BranchId { get; }
		IEnumerable<IItemFieldValue> SharedFields { get; }

		IEnumerable<IItemLanguage> UnversionedFields { get; }

		IEnumerable<IItemVersion> Versions { get; }

		IEnumerable<IItemData> GetChildren();
	}
}
