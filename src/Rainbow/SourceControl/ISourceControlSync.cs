namespace Rainbow.SourceControl
{
	public interface ISourceControlSync
	{
		bool CheckoutFileForDelete();
		bool CheckoutFileForEdit();
		bool AddFile();
	}
}