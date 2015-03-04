using System.IO;
using Sitecore.Data.Serialization.ObjectModel;

namespace Gibson.IO
{
	public class GibWriter
	{
		public virtual void WriteGib(SyncItem item, TextWriter writer)
		{
			// TODO: use custom format
			item.Serialize(writer);
		}
	}
}
