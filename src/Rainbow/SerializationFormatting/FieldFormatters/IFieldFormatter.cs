using Rainbow.Model;

namespace Rainbow.SerializationFormatting.FieldFormatters
{
	public interface IFieldFormatter
	{
		/// <remarks>The field passed may have a null type value.</remarks>
		bool CanFormat(ISerializableFieldValue field);
		string Format(ISerializableFieldValue field);
		/// <remarks>The value to unformat may be null.</remarks>
		string Unformat(string value);
	}
}
