using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sitecore.Diagnostics;

namespace Rainbow.Model
{
	public class ProxyItemLanguage : IItemLanguage
	{
		public ProxyItemLanguage(IItemLanguage baseLanguage)
		{
			Assert.ArgumentNotNull(baseLanguage, nameof(baseLanguage));

			Fields = baseLanguage.Fields.ToArray();
			Language = baseLanguage.Language;
		}

		public ProxyItemLanguage(CultureInfo language)
		{
			Assert.ArgumentNotNull(language, nameof(language));

			Language = language;
			Fields = Enumerable.Empty<IItemFieldValue>();
		}

		public IEnumerable<IItemFieldValue> Fields { get; set; }
		public CultureInfo Language { get; set; }
	}
}
