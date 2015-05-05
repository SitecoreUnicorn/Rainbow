using System;

namespace Gibson.Indexing
{
	public class IndexEntry
	{
		public string Path { get; set; }
		public Guid Id { get; set; }
		public Guid ParentId { get; set; }
		public Guid TemplateId { get; set; }
	}
}
