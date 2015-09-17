using System;
using System.Net;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Rainbow.SourceControl
{
	public class FileSyncTfs : ISourceControlSync
	{
		private readonly TfsPersistentConnection _connection;

		public FileSyncTfs(ScmSettings settings)
		{
			var networkCredential = new NetworkCredential(settings.Username, settings.Password, settings.Domain);
			var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(settings.ApplicationRootPath);
			AssertWorkspace(workspaceInfo, settings.ApplicationRootPath);

			_connection = TfsPersistentConnection.Instance(workspaceInfo.ServerUri, networkCredential);
		}

		private void AssertWorkspace(WorkspaceInfo workspaceInfo, string filename)
		{
			if (workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS Manager: No workspace is available or defined for the path " + filename);
		}

		public bool CheckoutFileForDelete(string filename)
		{
			var handler = new TfsFileHandler(_connection, filename);
			return handler.CheckoutFileForDelete();
		}

		public bool CheckoutFileForEdit(string filename)
		{
			var handler = new TfsFileHandler(_connection, filename);
			return handler.CheckoutFileForEdit();
		}

		public bool AddFile(string filename)
		{
			var handler = new TfsFileHandler(_connection, filename);
			return handler.AddFile();
		}
	}
}