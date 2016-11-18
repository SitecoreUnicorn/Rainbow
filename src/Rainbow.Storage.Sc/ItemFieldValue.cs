using System;
using System.Diagnostics;
using Rainbow.Model;
using Sitecore.Data.Fields;

namespace Rainbow.Storage.Sc
{
	[DebuggerDisplay("{NameHint} ({FieldType})")]
	public class ItemFieldValue : IItemFieldValue
	{
		private readonly Field _field;
		private readonly string _retrievedStringValue;

		public ItemFieldValue(Field field, string retrievedStringValue)
		{
			_field = field;
			_retrievedStringValue = retrievedStringValue;
		}

		public Guid FieldId => _field.ID.Guid;

		public virtual string Value
		{
			get
			{
				if (_field.IsBlobField)
				{
					if (!_field.HasBlobStream) return string.Empty;

					using (var stream = _field.GetBlobStream())
					{
						var buf = new byte[stream.Length];

						stream.Read(buf, 0, (int)stream.Length);

						return Convert.ToBase64String(buf);
					}
				}
				return _retrievedStringValue;
			}
		}

		public string FieldType => _field.Type;

		public virtual Guid? BlobId
		{
			get
			{
				if (_field.IsBlobField)
				{
					string parsedIdValue = _field.Value;
					if (parsedIdValue.Length > 38)
						parsedIdValue = parsedIdValue.Substring(0, 38);

					Guid blobId;
					if (Guid.TryParse(parsedIdValue, out blobId)) return blobId;
				}

				return null;
			}
		}

		public string NameHint => _field.Name;
	}
}
