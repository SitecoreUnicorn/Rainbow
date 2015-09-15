namespace Rainbow.SourceControl
{
	public interface ISourceControlSync
	{
		bool CheckoutFile();
		bool AddFile();
	}
}