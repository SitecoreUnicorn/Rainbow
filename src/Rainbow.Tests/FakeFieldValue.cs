using System;
using System.Diagnostics.CodeAnalysis;
using Rainbow.Model;

namespace Rainbow.Tests
{
	[ExcludeFromCodeCoverage]
	public class FakeFieldValue : IItemFieldValue
	{
		public FakeFieldValue(string value, string fieldType = "Test", Guid fieldId = new Guid(), string nameHint = "Fake test field")
		{
			Value = value;
			FieldType = fieldType;
			FieldId = fieldId;
			NameHint = nameHint;
		}

		public Guid FieldId { get; private set; }

		public string NameHint { get; private set; }

		public string Value { get; private set; }

		public string FieldType { get; private set; }
		public Guid? BlobId { get; private set; }
	}
}
