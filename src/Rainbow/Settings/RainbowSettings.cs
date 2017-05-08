namespace Rainbow.Settings
{
	/// <summary>
	/// This class represents the static options of Rainbow.
	/// You may override the settings provider (e.g. to setup Rainbow not using Sitecore's config factory)
	/// by setting the Current property _BEFORE USING ANY RAINBOW CLASS_ when your application is starting up.
	/// </summary>
	public class RainbowSettings
	{
		public virtual int SfsSerializationFolderPathMaxLength { get; } = 110;

		public virtual int SfsMaxItemNameLengthBeforeTruncation { get; } = 30;

		public virtual char[] SfsExtraInvalidFilenameCharacters { get; } = { '$' };

		public virtual string[] SfsInvalidFilenames { get; } = "CON,PRN,AUX,NUL,COM1,COM2,COM3,COM4,COM5,COM6,COM7,COM8,COM9,LPT1,LPT2,LPT3,LPT4,LPT5,LPT6,LPT7,LPT8,LPT9".Split(',');

		private static volatile RainbowSettings _current;
		private static readonly object SyncRoot = new object();
		public static RainbowSettings Current
		{
			get
			{
				if (_current != null) return _current;

				lock (SyncRoot)
				{
					if (_current != null) return _current;

					// we default to assuming we're in a Sitecore context
					return _current = new RainbowSitecoreSettings();
				}
			}
			set
			{
				lock (SyncRoot)
				{
					_current = value;
				}
			}
		}
	}
}
