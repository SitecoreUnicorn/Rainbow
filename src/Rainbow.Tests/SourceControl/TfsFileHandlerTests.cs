using System;
using Microsoft.TeamFoundation.Client;
using NSubstitute;
using Xunit;

namespace Rainbow.Tests.SourceControl
{
	public class TfsFileHandlerTests
	{
		private const string Filename = "edit-me.yml";
		private readonly Uri _uri = new Uri("http://google.com");

		[Fact]
		public void CheckoutFileForEdit_FileDoesNotExistOnServer_ThrowsException()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, false, false, false);

			Assert.Throws<Exception>(() => handler.CheckoutFileForEdit());
		}

		[Fact]
		public void AddFile_FileExistOnServer_ThrowsException()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, false);

			Assert.Throws<Exception>(() => handler.AddFile());
		}

		[Fact]
		public void AddFile_FileDoesNotExistOnFileSystem_ThrowsException()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, false, false);

			Assert.Throws<Exception>(() => handler.AddFile());
		}

		[Fact]
		public void AddFile_ZeroFilesUpdated_ReturnsFalse()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, false, true, false, 0);
			bool success = handler.AddFile();

			Assert.False(success);
		}

		[Fact]
		public void AddFile_OneFileUpdated_ReturnsFalse()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, false, true, false, 1);
			bool success = handler.AddFile();

			Assert.True(success);
		}

		[Fact]
		public void CheckoutFileForEdit_ZeroFilesUpdated_ReturnsFalse()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, false, 0);
			bool success = handler.CheckoutFileForEdit();

			Assert.False(success);
		}

		[Fact]
		public void CheckoutFileForEdit_OneFileUpdated_ReturnsFalse()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, false, 1);
			bool success = handler.CheckoutFileForEdit();

			Assert.True(success);
		}

		[Fact]
		public void CheckoutFileForDelete_ZeroFilesUpdated_ReturnsFalse()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, false, 0);
			bool success = handler.CheckoutFileForDelete();

			Assert.False(success);
		}

		[Fact]
		public void CheckoutFileForDelete_OneFileUpdated_ReturnsFalse()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, false, 1);
			bool success = handler.CheckoutFileForDelete();

			Assert.True(success);
		}

		[Fact]
		public void CheckoutFileForEdit_HasPendingEditChanges_ReturnsTrue()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, true, 0);
			bool success = handler.CheckoutFileForEdit();

			Assert.True(success);
		}

		[Fact]
		public void CheckoutFileForDelete_HasPendingEditChanges_ReturnsTrue()
		{
			var collection = Substitute.For<TfsTeamProjectCollection>(_uri);
			var handler = new TestableTfsFileHandler(collection, Filename, true, true, true, 0);
			bool success = handler.CheckoutFileForDelete();

			Assert.True(success);
		}
	}
}
