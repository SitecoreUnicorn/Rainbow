using System;

namespace Rainbow.Model
{
	public interface IItemFieldValue
	{
		Guid FieldId { get; }
		string NameHint { get; }
		string Value { get; }
		string FieldType { get; }
	}
}
