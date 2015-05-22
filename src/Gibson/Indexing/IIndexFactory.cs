namespace Gibson.Indexing
{
	public interface IIndexFactory
	{
		IIndex CreateIndex(string databaseName);
	}
}
