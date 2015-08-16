using System;
using System.Diagnostics;
using Rainbow.Model;

namespace Rainbow.Storage.Yaml.OutputModel
{
	[DebuggerDisplay("{Id} {Path} [YAML - {SerializedItemId}]")]
	public class YamlItemMetadata : IItemMetadata
	{
		public Guid Id { get; private set; }
		public Guid ParentId { get; private set; }
		public Guid TemplateId { get; private set; }
		public string Path { get; private set; }
		public string SerializedItemId { get; private set; }

		public virtual void ReadYaml(YamlReader reader, string serializedItemId)
		{
			Id = reader.ReadExpectedGuidMap("ID");
			ParentId = reader.ReadExpectedGuidMap("Parent");
			TemplateId = reader.ReadExpectedGuidMap("Template");
			Path = reader.ReadExpectedMap("Path");
			SerializedItemId = serializedItemId;
		}
	}
}
