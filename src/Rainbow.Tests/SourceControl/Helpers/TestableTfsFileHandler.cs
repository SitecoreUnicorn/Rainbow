using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Rainbow.SourceControl;

namespace Rainbow.Tests.SourceControl
{
	public class TestableTfsFileHandler : TfsFileHandler
	{
		private readonly bool _fileExistsOnServer;
		private readonly bool _fileExistsOnFileSystem;
		private readonly bool _hasPendingChanges;
		private readonly int _filesUpdated;
		
		public override bool FileExistsOnFileSystem { get { return _fileExistsOnFileSystem; } }
		public override bool FileExistsOnServer { get { return _fileExistsOnServer; } }

		public TestableTfsFileHandler(TfsTeamProjectCollection tfsTeamProjectCollection, string filename, bool fileExistsOnServer, bool fileExistsOnFileSystem, bool hasPendingChanges)
			: this(tfsTeamProjectCollection, filename, fileExistsOnServer, fileExistsOnFileSystem, hasPendingChanges, 0) { }

		public TestableTfsFileHandler(TfsTeamProjectCollection tfsTeamProjectCollection, string filename, bool fileExistsOnServer, bool fileExistsOnFileSystem, bool hasPendingChanges, int filesUpdated) : base(tfsTeamProjectCollection, filename)
		{
			_fileExistsOnFileSystem = fileExistsOnFileSystem;
			_fileExistsOnServer = fileExistsOnServer;
			_hasPendingChanges = hasPendingChanges;
			_filesUpdated = filesUpdated;
		}

		protected override bool GetFileExistsOnServer()
		{
			return _fileExistsOnServer;
		}

		protected override int AddFileToTfs()
		{
			return _filesUpdated;
		}

		protected override int DeleteFileInTfs()
		{
			return _filesUpdated;
		}

		protected override int EditFileInTfs()
		{
			return _filesUpdated;
		}

		protected override bool HasPendingChanges(ChangeType changeType)
		{
			return _hasPendingChanges;
		}

		protected override void UndoNonMatchingPendingChanges(ChangeType changeType)
		{
			// nothing to do here
			return;
		}
	}
}