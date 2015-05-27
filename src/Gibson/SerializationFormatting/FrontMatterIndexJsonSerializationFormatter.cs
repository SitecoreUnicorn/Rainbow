using System;
using System.IO;
using System.Text;
using Gibson.Indexing;
using Gibson.Model;
using Sitecore.Diagnostics;

namespace Gibson.SerializationFormatting
{
	public class FrontMatterIndexJsonSerializationFormatter : JsonSerializationFormatter
	{
		public override ISerializableItem ReadSerializedItem(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, "dataStream");
			var indexFrontMatter = ReadFrontMatter(dataStream);
			var jsonItem = base.ReadSerializedItem(dataStream, serializedItemId);
			jsonItem.AddIndexData(indexFrontMatter);

			return jsonItem;
		}

		public override void WriteSerializedItem(ISerializableItem item, Stream outputStream)
		{
			Assert.ArgumentNotNull(outputStream, "outputStream");

			WriteFrontMatter(outputStream, new IndexEntry().LoadFrom(item));
			base.WriteSerializedItem(item, outputStream);
		}

		/*
		 * Front matter is a fixed length data index set.
		 * Format (GUID is 36 chars):
		 * ID $guid\r\n
		 * PID $guid\r\n
		 * TID $guid\r\n
		 * PATH $path\r\n
		 * # begin JSON matter
		 */
		public IndexEntry ReadFrontMatter(Stream inputStream)
		{
			const int guidLength = 36;

			char[] idBuffer = new char[guidLength + 6];
			IndexEntry entry = new IndexEntry();

			using (var streamReader = new StreamReader(inputStream, Encoding.UTF8, false, 300, true))
			{
				Guid temp;

				// read ID
				streamReader.ReadBlock(idBuffer, 0, guidLength + 3); // 3 = "ID "
				if (!Guid.TryParse(new string(idBuffer, 3, guidLength), out temp)) throw new InvalidOperationException("The item ID GUID was not readable.");
				entry.Id = temp;

				// read PID
				streamReader.ReadBlock(idBuffer, 0, guidLength + 6); // 6 = "\r\nPID "
				if (!Guid.TryParse(new string(idBuffer, 6, guidLength), out temp)) throw new InvalidOperationException("The parent ID GUID was not readable.");
				entry.ParentId = temp;

				// read TID
				streamReader.ReadBlock(idBuffer, 0, guidLength + 6); // 6 = "\r\nTID"
				if (!Guid.TryParse(new string(idBuffer, 6, guidLength), out temp)) throw new InvalidOperationException("The template ID GUID was not readable.");
				entry.TemplateId = temp;

				// read PATH
				streamReader.ReadBlock(idBuffer, 0, 2); // skip \r\n at end of TID line
				entry.Path = streamReader.ReadLine().Substring(5);
			}

			// reset stream location to total read length, because StreamReader reads in buffer blocks
			// (idLen) + (pidlen) + (tidlen) + (pathlen) + (pathwrapperlen)
			inputStream.Seek(41 + 42 + 42 + entry.Path.Length + 7, SeekOrigin.Begin);

			return entry;
		}

		public void WriteFrontMatter(Stream outputStream, IndexEntry frontMatter)
		{
			StringBuilder buffer = new StringBuilder();

			buffer.AppendFormat("ID {0}\r\n", frontMatter.Id.ToString("D").ToUpperInvariant());
			buffer.AppendFormat("PID {0}\r\n", frontMatter.ParentId.ToString("D").ToUpperInvariant());
			buffer.AppendFormat("TID {0}\r\n", frontMatter.TemplateId.ToString("D").ToUpperInvariant());
			buffer.AppendFormat("PATH {0}\r\n", frontMatter.Path);

			outputStream.Write(Encoding.UTF8.GetBytes(buffer.ToString()), 0, buffer.Length);
		}
	}
}
