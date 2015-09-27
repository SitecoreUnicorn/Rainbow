using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sitecore;

// ReSharper disable DoNotCallOverridableMethodsInConstructor

namespace Rainbow.Model
{
	/// <summary>
	/// Copies any IItemVersion into a proxy object, fully evaluating any lazy loading
	/// and enabling permuting the values in the version
	/// </summary>
	public class ProxyItemVersion : IItemVersion
	{
		public ProxyItemVersion(IItemVersion versionToProxy)
		{
			VersionNumber = versionToProxy.VersionNumber;
			Language = versionToProxy.Language;
			Fields = versionToProxy.Fields.Select(field => new ProxyFieldValue(field)).ToArray();
		}

		public ProxyItemVersion(CultureInfo language, int versionNumber)
		{
			Language = language;
			VersionNumber = versionNumber;
			Fields = new IItemFieldValue[] { new ProxyFieldValue(FieldIDs.Created.Guid, DateUtil.IsoNowWithTicks) };
		}

		public virtual IEnumerable<IItemFieldValue> Fields { get; set; }
		public virtual CultureInfo Language { get; set; }
		public virtual int VersionNumber { get; set; }
	}
}
