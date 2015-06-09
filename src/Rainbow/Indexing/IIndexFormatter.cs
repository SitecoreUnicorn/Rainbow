using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Rainbow.Indexing
{
	public interface IIndexFormatter
	{
		ReadOnlyCollection<IndexEntry> ReadIndex(Stream sourceStream);
		void WriteIndex(IEnumerable<IndexEntry> entries, Stream outputStream);
	}
}
