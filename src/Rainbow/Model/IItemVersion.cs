namespace Rainbow.Model
{
	/// <summary>
	/// A version of an item in a specific language
	/// </summary>
	public interface IItemVersion : IItemLanguage
	{
		int VersionNumber { get; }
	}
}
