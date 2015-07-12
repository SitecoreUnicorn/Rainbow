using System.Collections.Generic;
using System.Globalization;

namespace Rainbow.Model
{
	public interface IItemVersion
	{
		IEnumerable<IItemFieldValue> Fields { get; }
		CultureInfo Language { get; }
		int VersionNumber { get; }
	}
}
