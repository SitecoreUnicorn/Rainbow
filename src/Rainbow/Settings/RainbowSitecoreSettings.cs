namespace Rainbow.Settings
{
	/// <summary>
	/// Implements Rainbow's settings using Sitecore's settings factory. This is the default implementation of settings.
	/// </summary>
	public class RainbowSitecoreSettings : RainbowSettings
	{
		public override char[] SfsExtraInvalidFilenameCharacters =>
			Sitecore.Configuration.Settings
			.GetSetting("Rainbow.SFS.ExtraInvalidFilenameCharacters", string.Empty)
			.ToCharArray();

		public override string[] SfsInvalidFilenames =>
			Sitecore.Configuration.Settings.GetSetting("Rainbow.SFS.InvalidFilenames", "CON,PRN,AUX,NUL,COM1,COM2,COM3,COM4,COM5,COM6,COM7,COM8,COM9,LPT1,LPT2,LPT3,LPT4,LPT5,LPT6,LPT7,LPT8,LPT9")
			.Split(',');

		public override int SfsSerializationFolderPathMaxLength => Sitecore.Configuration.Settings.GetIntSetting("Rainbow.SFS.SerializationFolderPathMaxLength", 110);

		public override int SfsMaxItemNameLengthBeforeTruncation => Sitecore.Configuration.Settings.GetIntSetting("Rainbow.SFS.MaxItemNameLengthBeforeTruncation", 30);
	}
}
