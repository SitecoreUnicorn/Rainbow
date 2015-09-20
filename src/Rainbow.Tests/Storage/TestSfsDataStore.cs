using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Storage;
using Rainbow.Storage.Yaml;
using Rainbow.Tests.SourceControl;

namespace Rainbow.Tests.Storage
{
	internal class TestSfsDataStore : SerializationFileSystemDataStore, IDisposable
	{
		public TestSfsDataStore(params string[] rootPaths) : 
			base(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), false, new FakeRootFactory(rootPaths), new YamlSerializationFormatter(null, null), new TestableSourceControlManager(true))
		{
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (PhysicalRootPath != null && Directory.Exists(PhysicalRootPath))
				Directory.Delete(PhysicalRootPath, true);
		}

		private class FakeRootFactory : ITreeRootFactory
		{
			private readonly IEnumerable<string> _roots;

			public FakeRootFactory(IEnumerable<string> roots)
			{
				_roots = roots;
			}

			public IEnumerable<TreeRoot> CreateTreeRoots()
			{
				return _roots.Select((root, idx) => new TreeRoot("T" + idx, root, "master"));
			}
		}
	}
}
