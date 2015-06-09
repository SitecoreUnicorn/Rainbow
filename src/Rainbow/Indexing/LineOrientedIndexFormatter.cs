using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;

namespace Rainbow.Indexing
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
					if (line.Length < 1)
					{
						line = reader.ReadLine();
						continue;
					}

					if (line[0] == '-' || currentItem == null)
					{
						if (currentItem != null) results.Add(currentItem);

						currentItem = new IndexEntry();

						line = reader.ReadLine();
						continue;
					}

					switch (line[0])
					{
						case '/':
							currentItem.Path = line;
							break;
						case 'T':
							currentItem.TemplateId = new Guid(line.Substring(3));
							break;
						case 'P':
							currentItem.ParentId = new Guid(line.Substring(3));
							break;
						default:
							if(line.Length == 36)
								currentItem.Id = new Guid(line);

							break;
					}

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
					writer.WriteLine(item.Id.ToString("D").ToUpperInvariant());
					writer.WriteLine(item.Path);
					writer.WriteLine("TPL " + item.TemplateId.ToString("D").ToUpperInvariant());
					writer.WriteLine("PID " + item.ParentId.ToString("D").ToUpperInvariant());
				}
			}
		}
	}
}
