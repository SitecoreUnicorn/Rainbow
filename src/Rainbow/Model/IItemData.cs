using System;
using System.Collections.Generic;

namespace Rainbow.Model
{
	public interface IItemData : IItemMetadata
	{
		string DatabaseName { get; set; }
		string Name { get; }
		Guid BranchId { get; }
		IEnumerable<IItemFieldValue> SharedFields { get; }
		IEnumerable<IItemVersion> Versions { get; }

		IEnumerable<IItemData> GetChildren();
	}
}
