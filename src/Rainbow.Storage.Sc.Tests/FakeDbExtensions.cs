using Sitecore.Data.Items;
using Sitecore.FakeDb;

namespace Rainbow.Storage.Sc.Tests
{
	public static class FakeDbExtensions
	{
		public static Item CreateItem(this Db db, DbItem item)
		{
			db.Add(item);

			return db.GetItem(item.ID);
		}
	}
}
