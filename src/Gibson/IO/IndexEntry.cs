using Sitecore.Data;

namespace Gibson.IO
{
	public class IndexEntry
	{
		public string Path { get; set; }
		public ID Id { get; set; }
		public ID ParentId { get; set; }
		public ID TemplateId { get; set; }
	}
}
