using System;

namespace Gibson.Model
{
	public interface ISerializableFieldValue
	{
		Guid FieldId { get; }
		string NameHint { get; }
		string Value { get; }
		string FieldType { get; }
	}
}
