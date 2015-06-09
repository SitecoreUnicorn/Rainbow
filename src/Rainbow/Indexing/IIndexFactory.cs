namespace Rainbow.Indexing
{
	public interface IIndexFactory
	{
		IIndex CreateIndex(string databaseName);
	}
}
