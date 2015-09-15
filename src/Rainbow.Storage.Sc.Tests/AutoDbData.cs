using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit2;
using Sitecore.FakeDb.AutoFixture;

namespace Rainbow.Storage.Sc.Tests
{
	internal class AutoDbDataAttribute : AutoDataAttribute
	{
		public AutoDbDataAttribute()
		  : base(new Fixture().Customize(new AutoDbCustomization()))
		{
		}
	}
}
