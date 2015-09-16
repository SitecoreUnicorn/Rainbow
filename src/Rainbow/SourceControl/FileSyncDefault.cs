namespace Rainbow.SourceControl
{
	public class FileSyncDefault : ISourceControlSync
	{
		public bool CheckoutFileForDelete()
		{
			return true;
		}

		public bool CheckoutFileForEdit()
		{
			return true;
		}

		public bool AddFile()
		{
			return true;
		}
	}
}