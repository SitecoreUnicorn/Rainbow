using System;
using System.IO;
using System.Text;
using System.Xml;
using Gibson.Indexing;
using Gibson.Model;
using Gibson.SerializationFormatting.Yaml;
using Sitecore.Diagnostics;

namespace Gibson.SerializationFormatting
{
	public class FrontMatterIndexJsonSerializationFormatter : YamlSerializationFormatter
	{
		public FrontMatterIndexJsonSerializationFormatter()
		{
			
		}

		public FrontMatterIndexJsonSerializationFormatter(XmlNode configNode) : base(configNode)
		{
			
		}

		public override ISerializableItem ReadSerializedItem(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, "dataStream");
		//	var indexFrontMatter = ReadFrontMatter(dataStream);
			var jsonItem = base.ReadSerializedItem(dataStream, serializedItemId);
			//jsonItem.AddIndexData(indexFrontMatter);

			return jsonItem;
		}

		public override void WriteSerializedItem(ISerializableItem item, Stream outputStream)
		{
			Assert.ArgumentNotNull(outputStream, "outputStream");

			//WriteFrontMatter(outputStream, new IndexEntry().LoadFrom(item));
			base.WriteSerializedItem(item, outputStream);
		}

		/*
		 * Front matter is a fixed length data index set.
		 * Format (GUID is 36 chars):
		 * ID $guid\r\n
		 * PID $guid\r\n
		 * TID $guid\r\n
		 * PATH $path\r\n
		 * # begin regular matter
		 */
		public IndexEntry ReadFrontMatter(Stream inputStream)
		{
			var entry = new IndexEntry();
			using (var reader = new YamlReader(inputStream, 300, true))
			{
				entry.Id = reader.ReadExpectedGuidMap("ID");
				entry.ParentId = reader.ReadExpectedGuidMap("Parent");
				entry.TemplateId = reader.ReadExpectedGuidMap("Template");
				entry.Path = reader.ReadExpectedMap("Path");
			}

			return entry;
		}
	}
}
