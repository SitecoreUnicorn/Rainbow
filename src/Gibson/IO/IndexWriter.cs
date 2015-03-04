using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gibson.IO
{
	public class IndexWriter
	{
		public void WriteGibs(IEnumerable<IndexEntry> entries, TextWriter writer)
		{
			foreach (var item in entries.OrderBy(x => x.Path))
			{
				writer.WriteLine("-- Item --");
				writer.WriteLine("PATH " + item.Path);
				writer.WriteLine("ID " + item.Id);
				writer.WriteLine("TEMPLATE " + item.TemplateId);
				writer.WriteLine("ANCESTOR " + item.ParentId);
			}
		}
	}
}
