using System;
using System.Net;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Rainbow.SourceControl
{
	public class FileSyncTfs : ISourceControlSync
	{
		private readonly WorkspaceInfo _workspaceInfo;
		private readonly NetworkCredential _networkCredential;

		public FileSyncTfs(ScmSettings settings)
		{
			_networkCredential = new NetworkCredential(settings.Username, settings.Password, settings.Domain);
			_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(settings.ApplicationRootPath);
			AssertWorkspace(_workspaceInfo, settings.ApplicationRootPath);
		}

		private void AssertWorkspace(WorkspaceInfo workspaceInfo, string filename)
		{
			if (workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS Manager: No workspace is available or defined for the path " + filename);
		}

		private TfsPersistentConnection GetTfsPersistentConnection()
		{
			return TfsPersistentConnection.Instance(_workspaceInfo.ServerUri, _networkCredential);
		}

		public bool CheckoutFileForDelete(string filename)
		{
			var connection = GetTfsPersistentConnection();
			var handler = new TfsFileHandler(connection.TfsTeamProjectCollection, filename);
			return handler.CheckoutFileForDelete();
		}

		public bool CheckoutFileForEdit(string filename)
		{
			var connection = GetTfsPersistentConnection();
			var handler = new TfsFileHandler(connection.TfsTeamProjectCollection, filename);
			return handler.CheckoutFileForEdit();
		}

		public bool AddFile(string filename)
		{
			var connection = GetTfsPersistentConnection();
			var handler = new TfsFileHandler(connection.TfsTeamProjectCollection, filename);
			return handler.CheckoutFileForEdit();
		}
	}
}