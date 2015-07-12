using System;
using Rainbow.Model;

namespace Rainbow.Indexing
{
	public class IndexEntry
	{
		public string Path { get; set; }
		public Guid Id { get; set; }
		public Guid ParentId { get; set; }
		public Guid TemplateId { get; set; }

		public string Name
		{
			get
			{
				if (Path == null) return null;

				var trimmedPath = Path.TrimEnd('/');

				var index = trimmedPath.LastIndexOf('/');

				if (index < 0) return Path;

				return trimmedPath.Substring(index + 1);
			}
		}

		public string ParentPath
		{
			get
			{
				var name = Name;
				if (name == null) return null;

				var trimmedPath = Path.TrimEnd('/');

				var result = trimmedPath.Substring(0, trimmedPath.Length - (name.Length + 1));

				if (result.Equals(string.Empty)) return null;

				return result;
			}
		}

		public IndexEntry LoadFrom(IItemData itemData)
		{
			Path = itemData.Path;
			Id = itemData.Id;
			ParentId = itemData.ParentId;
			TemplateId = itemData.TemplateId;

			return this;
		}

		public IndexEntry Clone()
		{
			return new IndexEntry { Path = Path, Id = Id, ParentId = ParentId, TemplateId = TemplateId };
		}
	}
}
