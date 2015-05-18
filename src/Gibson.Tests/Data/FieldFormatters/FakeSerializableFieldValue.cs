using System;
using Gibson.Model;

namespace Gibson.Tests.Data.FieldFormatters
{
	public class FakeSerializableFieldValue : ISerializableFieldValue
	{
		public FakeSerializableFieldValue(string value, string fieldType = "Test", Guid fieldId = new Guid())
		{
			Value = value;
			FieldType = fieldType;
			FieldId = fieldId;
		}

		public Guid FieldId { get; private set; }

		public string Value { get; private set; }

		public string FieldType { get; private set; }
	}
}
