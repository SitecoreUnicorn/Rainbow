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
		}

		public Guid FieldId { get; private set; }

		public string NameHint { get; private set; }

		public string Value { get; private set; }

		public string FieldType { get; private set; }
	}
}
