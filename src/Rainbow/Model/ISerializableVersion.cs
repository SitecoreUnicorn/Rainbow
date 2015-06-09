using System.Collections.Generic;
using System.Globalization;

namespace Rainbow.Model
{
	public interface ISerializableVersion
	{
		IEnumerable<ISerializableFieldValue> Fields { get; }
		CultureInfo Language { get; }
		int VersionNumber { get; }
	}
}
