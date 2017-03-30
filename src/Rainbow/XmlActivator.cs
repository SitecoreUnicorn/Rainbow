using System;
using System.Xml;

namespace Rainbow
{
	public static class XmlActivator
	{
		/// <summary>
		/// Activates a type defined in an XML node. Analog to Sitecore's Factory.CreateObject() but without the fancy params support.
		/// This exists because Factory.CreateObject() gained all sorts of dependencies on MS DI in Sitecore 8.2 that make it inoperative
		/// without a Sitecore context.
		/// </summary>
		public static T CreateObject<T>(XmlNode node)
			where T : class
		{
			var typeString = node.Attributes?["type"]?.InnerText;
			if (typeString == null) return null;

			var type = Type.GetType(typeString);

			if (type == null) return null;

			return Activator.CreateInstance(type) as T;
		}
	}
}
