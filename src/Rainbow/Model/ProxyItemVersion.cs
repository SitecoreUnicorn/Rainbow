using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rainbow.Model
{
	public class ProxyItemVersion : IItemVersion
	{
		public ProxyItemVersion(IItemVersion versionToProxy)
		{
			VersionNumber = versionToProxy.VersionNumber;
			Language = versionToProxy.Language;
			Fields = versionToProxy.Fields.Select(field => new ProxyFieldValue(field)).ToArray();
		}

		public IEnumerable<IItemFieldValue> Fields { get; private set; }
		public CultureInfo Language { get; private set; }
		public int VersionNumber { get; private set; }
	}
}
