namespace Rainbow.SourceControl
{
	public class FileSyncDefault : ISourceControlSync
	{
		public bool CheckoutFileForDelete(string filename)
		{
			return true;
		}

		public bool CheckoutFileForEdit(string filename)
		{
			return true;
		}

		public bool AddFile(string filename)
		{
			return true;
		}
	}
}