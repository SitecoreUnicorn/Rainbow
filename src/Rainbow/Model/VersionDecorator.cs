using System.Collections.Generic;
using System.Globalization;

namespace Rainbow.Model
{
	public abstract class VersionDecorator : IItemVersion
	{
		protected readonly IItemVersion InnerVersion;

		protected VersionDecorator(IItemVersion innerVersion)
		{
			InnerVersion = innerVersion;
		}

		public virtual IEnumerable<IItemFieldValue> Fields { get { return InnerVersion.Fields; } }
		public virtual CultureInfo Language { get { return InnerVersion.Language; } }
		public virtual int VersionNumber { get { return InnerVersion.VersionNumber; } }
	}
}
