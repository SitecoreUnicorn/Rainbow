using System;
using System.Collections.Generic;

namespace Rainbow.Filtering
{
	// a field filter which can have the fields it excludes be enumerated
	public interface IEnumerableFieldFilter
	{
		IEnumerable<Guid> Excludes { get; }
	}
}
