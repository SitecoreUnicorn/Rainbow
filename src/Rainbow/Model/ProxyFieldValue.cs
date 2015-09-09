using System;

namespace Rainbow.Model
{
	public class ProxyFieldValue : IItemFieldValue
	{
		public ProxyFieldValue(IItemFieldValue fieldToProxy)
		{
			Value = fieldToProxy.Value;
			FieldType = fieldToProxy.FieldType;
			FieldId = fieldToProxy.FieldId;
			NameHint = fieldToProxy.NameHint;
			BlobId = fieldToProxy.BlobId;
		}

		public Guid FieldId { get; set; }

		public string NameHint { get; set; }

		public string Value { get; set; }

		public string FieldType { get; set; }
		public Guid? BlobId { get; set; }
	}
}
