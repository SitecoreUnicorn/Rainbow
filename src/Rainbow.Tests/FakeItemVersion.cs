using System.Collections.Generic;
using System.Globalization;
using Rainbow.Model;

namespace Rainbow.Tests
{
	public class FakeItemVersion : IItemVersion
	{
		public FakeItemVersion(int versionNumber = 1, string language = "en", params IItemFieldValue[] fields)
		{
			VersionNumber = versionNumber;
			Language = new CultureInfo(language);
			Fields = fields;
		}

		public IEnumerable<IItemFieldValue> Fields { get; private set; }
		public CultureInfo Language { get; private set; }
		public int VersionNumber { get; private set; }
	}
}
