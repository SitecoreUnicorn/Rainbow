using System.Collections.Generic;
using System.Linq;

namespace Rainbow
{
	public static class DocumentationUtility
	{
		public static string GetFriendlyName(object instance)
		{
			var documentable = instance as IDocumentable;

			if (documentable == null) return instance.GetType().Name;

			return documentable.FriendlyName;
		}

		public static string GetDescription(object instance)
		{
			var documentable = instance as IDocumentable;

			if (documentable == null) return instance.GetType().AssemblyQualifiedName;

			return documentable.Description;
		}

		public static KeyValuePair<string, string>[] GetConfigurationDetails(object instance)
		{
			var documentable = instance as IDocumentable;

			if (documentable == null) return new KeyValuePair<string, string>[0];

			return documentable.GetConfigurationDetails();
		}
	}
}
