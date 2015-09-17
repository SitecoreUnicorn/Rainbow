namespace Rainbow.SourceControl
{
	public interface ISourceControlSync
	{
		bool CheckoutFileForDelete(string filename);
		bool CheckoutFileForEdit(string filename);
		bool AddFile(string filename);
	}
}