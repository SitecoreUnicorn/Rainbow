using System;
using Rainbow.Model;

namespace Rainbow.Tests.Formatting.FieldFormatters
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

		public string NameHint
		{
			get { return "Fake test field"; }
		}

		public string Value { get; private set; }

		public string FieldType { get; private set; }
	}
}
