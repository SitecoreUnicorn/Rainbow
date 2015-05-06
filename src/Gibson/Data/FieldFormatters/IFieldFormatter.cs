using Gibson.Model;

namespace Gibson.Data.FieldFormatters
{
	public interface IFieldFormatter
	{
		bool CanFormat(ISerializableFieldValue field);
		string Format(ISerializableFieldValue field);
		string Unformat(string value);
	}
}
