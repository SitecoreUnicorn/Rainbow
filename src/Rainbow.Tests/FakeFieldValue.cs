using System;
using System.Diagnostics.CodeAnalysis;
using Rainbow.Model;

namespace Rainbow.Tests
{
	[ExcludeFromCodeCoverage]
	public class FakeFieldValue : ProxyFieldValue
	{
		public FakeFieldValue(string value, string fieldType = "Test", Guid fieldId = new Guid(), string nameHint = "Fake test field", Guid? blobId = null) : base(fieldId, value)
		{
			FieldType = fieldType;
			NameHint = nameHint;
			BlobId = blobId;
		}
	}
}
