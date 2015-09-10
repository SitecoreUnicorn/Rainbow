using System;
// ReSharper disable DoNotCallOverridableMethodsInConstructor

namespace Rainbow.Model
{
	/// <summary>
	/// Copies any IItemFieldValue into a proxy object, fully evaluating any lazy loading
	/// and enabling permuting the values in the field
	/// </summary>
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

		public virtual Guid FieldId { get; set; }

		public virtual string NameHint { get; set; }

		public virtual string Value { get; set; }

		public virtual string FieldType { get; set; }
		public virtual Guid? BlobId { get; set; }
	}
}
