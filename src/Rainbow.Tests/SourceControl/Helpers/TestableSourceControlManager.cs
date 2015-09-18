using Rainbow.SourceControl;

namespace Rainbow.Tests.SourceControl
{
	public class TestableSourceControlManager : SourceControlManager
	{
		public TestableSourceControlManager(bool success) : base(new TestableTfsFileSync(success)) { }
	}
}