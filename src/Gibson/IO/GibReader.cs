using System.IO;
using Sitecore.Data.Serialization.ObjectModel;

namespace Gibson.IO
{
	public class GibReader
	{
		public virtual SyncItem ReadGib(TextReader textReader)
		{
			// note: can tweak the way items are read by replicating SyncItem.ReadItem()
			// perhaps, say, ignoring the path, adding comments?

			// format notes:
			// - we can infer database from the index
			// - we can infer path from the index, though it's nice to have for reference/merges
			// - we can infer parent from the index
			// - we can infer template ID from the index, though nice to have (as is templatekey)

			// field format notes
			// - we don't need key
			// - name is nice to have for readability, so keep it
			// - we can omit revision, updated, updated by

			// version format notes
			// - we don't need revision. we're storing data here.
			// TODO: use custom format
			return SyncItem.ReadItem(new Tokenizer(textReader));
		}
	}
}
