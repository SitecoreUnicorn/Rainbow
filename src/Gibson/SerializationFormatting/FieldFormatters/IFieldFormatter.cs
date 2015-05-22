using Gibson.Model;

namespace Gibson.SerializationFormatting.FieldFormatters
{
	public interface IFieldFormatter
	{
		bool CanFormat(ISerializableFieldValue field);
		string Format(ISerializableFieldValue field);
		string Unformat(string value);
	}
}
