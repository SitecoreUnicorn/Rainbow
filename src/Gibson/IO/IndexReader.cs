using System.Collections.Generic;
using System.IO;
using Sitecore.Data;

namespace Gibson.IO
{
	public class IndexReader
	{
		public IReadOnlyCollection<IndexEntry> ReadGibs(TextReader reader)
		{
			var results = new List<IndexEntry>();

			IndexEntry currentItem = null;
			string line = reader.ReadLine();

			while (line != null)
			{
				if (line[0] == '-')
				{
					if (currentItem != null) results.Add(currentItem);

					currentItem = new IndexEntry();
				}

				if (line[0] == 'P') currentItem.Path = line.Substring(5);
				if (line[0] == 'I') currentItem.Id = new ID(line.Substring(3));
				if (line[0] == 'T') currentItem.TemplateId = new ID(line.Substring(9));
				if (line[0] == 'A') currentItem.ParentId = new ID(line.Substring(9));

				line = reader.ReadLine();
			}

			// add the last item in the file
			if(currentItem != null) results.Add(currentItem);

			return results.AsReadOnly();
		}
	}
}
