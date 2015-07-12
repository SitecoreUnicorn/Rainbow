using Rainbow.Model;

namespace Rainbow.Formatting.FieldFormatters
{
	public interface IFieldFormatter
	{
		/// <remarks>The field passed may have a null type value.</remarks>
		bool CanFormat(IItemFieldValue field);
		string Format(IItemFieldValue field);
		/// <remarks>The value to unformat may be null.</remarks>
		string Unformat(string value);
	}
}
