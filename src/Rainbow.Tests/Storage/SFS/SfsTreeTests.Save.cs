using System;
using System.IO;
using NUnit.Framework;

namespace Rainbow.Tests.Storage.SFS
{
	partial class SfsTreeTests
	{
		[Test]
		public void Save_WritesItem_WhenItemIsRoot_AndTreeIsAtRoot()
		{
			throw new NotImplementedException();
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				testTree.Remove(testTree.GetRootItem());

				Assert.IsEmpty(Directory.GetFileSystemEntries(testTree.PhysicalRootPathTest));

				var root = testTree.GetRootItem();

				Assert.IsNull(root);
			}
		}

		[Test]
		public void Save_WritesItem_WhenItemIsNested_AndTreeIsAtRoot()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenItemIsRoot_AndTreeIsNested()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenItemIsNested_AndTreeIsNested()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresLoopbackFolder()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresChildOfLoopbackFolder()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresDoubleLoopbackFolder()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenPathRequiresChildOfDoubleLoopbackFolder()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Save_WritesItem_WhenItemNameIsFullOfInvalidChars()
		{
			throw new NotImplementedException();
		}
	}
}
