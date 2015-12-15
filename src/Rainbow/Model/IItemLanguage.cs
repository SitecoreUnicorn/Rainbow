using System.Collections.Generic;
using System.Globalization;

namespace Rainbow.Model
{
	/// <summary>
	/// A language for an item.
	/// This can be either:
	/// 1) The basis for a language's unversioned fields
	/// 2) The basis for an <see cref="IItemVersion"/> in a language
	/// </summary>
	public interface IItemLanguage
	{
		IEnumerable<IItemFieldValue> Fields { get; }

		CultureInfo Language { get; }
	}
}
