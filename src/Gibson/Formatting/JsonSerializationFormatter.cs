using System;
using System.Collections.Generic;
using System.IO;
using Gibson.Formatting.FieldFormatters;
using Gibson.Formatting.Json;
using Gibson.Model;

namespace Gibson.Formatting
{
	public class JsonSerializationFormatter : ISerializationFormatter
	{
		private readonly List<IFieldFormatter> _fieldFormatters = new List<IFieldFormatter>();
		public List<IFieldFormatter> FieldFormatters
		{
			get
			{
				return _fieldFormatters;
			}
		}


		public ISerializableItem ReadSerializedItem(Stream dataStream)
		{
			throw new NotImplementedException();
		}

		public void WriteSerializedItem(ISerializableItem item, Stream outputStream)
		{
			throw new NotImplementedException();
		}

		protected JsonFieldValue FormatFieldValue(ISerializableFieldValue field)
		{
			string value = field.Value;

			for (int i = 0; i < FieldFormatters.Count; i++)
			{
				var formatter = FieldFormatters[i];
				if (formatter.CanFormat(field))
				{
					value = formatter.Format(field);
					break;
				}
			}

			return new JsonFieldValue
			{
				Id = field.FieldId,
				Type = field.FieldType,
				Value = value
			};
		}
	}
}
