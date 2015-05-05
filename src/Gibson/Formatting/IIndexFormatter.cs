using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Gibson.Indexing;

namespace Gibson.Formatting
{
	public interface IIndexFormatter
	{
		ReadOnlyCollection<IndexEntry> ReadIndex(Stream sourceStream);
		void WriteIndex(IEnumerable<IndexEntry> entries, Stream outputStream);
	}
}
