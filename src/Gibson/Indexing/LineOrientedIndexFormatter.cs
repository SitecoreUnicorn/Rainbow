using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;

namespace Gibson.Indexing
{
	public class LineOrientedIndexFormatter : IIndexFormatter
	{
		public ReadOnlyCollection<IndexEntry> ReadIndex(Stream sourceStream)
		{
			Assert.ArgumentNotNull(sourceStream, "sourceStream");
			
			using (var reader = new StreamReader(sourceStream))
			{
				var results = new List<IndexEntry>();

				IndexEntry currentItem = null;
				string line = reader.ReadLine();

				while (line != null)
				{
					if (line[0] == '-' || currentItem == null)
					{
						if (currentItem != null) results.Add(currentItem);

						currentItem = new IndexEntry();
					}

					if (line[0] == 'P') currentItem.Path = line.Substring(5);
					if (line[0] == 'I') currentItem.Id = new Guid(line.Substring(3));
					if (line[0] == 'T') currentItem.TemplateId = new Guid(line.Substring(9));
					if (line[0] == 'A') currentItem.ParentId = new Guid(line.Substring(9));

					line = reader.ReadLine();
				}

				// add the last item in the file
				if (currentItem != null) results.Add(currentItem);

				return results.AsReadOnly();
			}
		}

		public void WriteIndex(IEnumerable<IndexEntry> entries, Stream outputStream)
		{
			Assert.ArgumentNotNull(entries, "entries");
			Assert.ArgumentNotNull(outputStream, "outputStream");

			// NOTE: leaving the output stream open - it's up to the caller to dispose that.
			using (var writer = new StreamWriter(outputStream, Encoding.UTF8, 1024, true))
			{
				foreach (var item in entries.OrderBy(x => x.Path))
				{
					writer.WriteLine("-- Item --");
					writer.WriteLine("PATH " + item.Path);
					writer.WriteLine("ID " + item.Id.ToString("D").ToUpperInvariant());
					writer.WriteLine("TEMPLATE " + item.TemplateId.ToString("D").ToUpperInvariant());
					writer.WriteLine("ANCESTOR " + item.ParentId.ToString("D").ToUpperInvariant());
				}
			}
		}
	}
}
