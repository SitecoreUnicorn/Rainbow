using System;
using System.Collections.Generic;

namespace Gibson.Model
{
	public interface ISerializableItem
	{
		Guid Id { get; }
		string DatabaseName { get; }
		Guid ParentId { get; }
		string Name { get; }
		Guid BranchId { get; }
		Guid TemplateId { get; }
		IEnumerable<ISerializableFieldValue> SharedFields { get; }
		IEnumerable<ISerializableVersion> Versions { get; } 
	}
}
