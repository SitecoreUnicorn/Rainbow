using System;

namespace Rainbow.Model
{
	public interface IItemMetadata
	{
		Guid Id { get; }
		Guid ParentId { get; }
		Guid TemplateId { get; }
		string Path { get; }

		/// <summary>
		/// Provider-specific identifier for a serialized item (e.g. path on disk, row identifier, etc)
		/// </summary>
		string SerializedItemId { get; }

	}
}
