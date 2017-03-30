using System.Globalization;
using Rainbow.Model;

namespace Rainbow.Tests
{
	public class FakeItemVersion : ProxyItemVersion
	{
		public FakeItemVersion(int versionNumber = 1, string language = "en", params IItemFieldValue[] fields) : base(new CultureInfo(language), versionNumber)
		{
			// ReSharper disable once VirtualMemberCallInConstructor
			Fields = fields;
		}
	}
}
