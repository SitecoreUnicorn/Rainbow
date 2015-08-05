using System.Collections.Generic;

namespace Rainbow
{
	/// <summary>
	/// An interface that allows documentation to be generated from the implementing type.
	/// 
	/// This is used to allow displaying friendly details about an object when displayed in a UI.
	/// </summary>
	public interface IDocumentable
	{
		string FriendlyName { get; }
		string Description { get; }
		KeyValuePair<string, string>[] GetConfigurationDetails();
	}
}
