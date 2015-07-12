using System;

namespace Rainbow.Filtering
{
	/// <summary>
	/// The Field Filter is a way to exclude certain fields from being controlled by Rainbow.
	/// Comparers, serialization formatters, and deserializers should check for filtering before comparing or writing values
	/// </summary>
	public interface IFieldFilter
	{
		bool Includes(Guid fieldId);
	}
}
