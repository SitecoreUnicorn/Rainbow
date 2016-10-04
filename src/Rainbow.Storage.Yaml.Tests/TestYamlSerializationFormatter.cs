using System.Xml;
using Rainbow.Filtering;
using Rainbow.Formatting.FieldFormatters;

namespace Rainbow.Storage.Yaml.Tests
{
	class TestYamlSerializationFormatter : YamlSerializationFormatter
	{
		public TestYamlSerializationFormatter(XmlNode configNode, IFieldFilter fieldFilter) : base(configNode, fieldFilter)
		{
		}

		public TestYamlSerializationFormatter(IFieldFilter fieldFilter, params IFieldFormatter[] fieldFormatters) : base(fieldFilter, fieldFormatters)
		{
		}
	}
}
