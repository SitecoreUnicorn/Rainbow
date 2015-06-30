using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Diagnostics;

namespace Rainbow.Filtering
{
	/// <summary>
	/// FieldPredicate that reads from an XML config body to define field exclusions, e.g.
	/// <exclude fieldID="{79FA7A4C-378F-425B-B50E-EE2FCC5A8FEF}" note="/sitecore/templates/Sites/Example Site/Subcontent/Test Template/Test/Title" />
	/// </summary>
	public class ConfigurationFieldFilter : IFieldFilter
	{
		private readonly HashSet<Guid> _excludedFieldIds = new HashSet<Guid>();
		private readonly HashSet<string> _excludedFieldIdStrings = new HashSet<string>(); 
 
		public ConfigurationFieldFilter(XmlNode configNode)
		{
			Assert.IsNotNull(configNode, "configNode");
			
			foreach (var element in configNode.ChildNodes.OfType<XmlElement>())
			{
				var attribute = element.Attributes["fieldID"];
				Guid candidate;
				if (attribute == null || !Guid.TryParse(attribute.InnerText, out candidate)) continue;

				_excludedFieldIds.Add(candidate);
				_excludedFieldIdStrings.Add(candidate.ToString());
			}
		}

		public bool Includes(Guid fieldId)
		{
			return !_excludedFieldIds.Contains(fieldId);
		}

		public bool Includes(string fieldId)
		{
			return !_excludedFieldIdStrings.Contains(fieldId);
		}
	}
}
