using System;
using System.IO;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Rainbow.SourceControl
{
	public class TfsFileSync : ISourceControlSync
	{
		private readonly string _filename;
		private readonly NetworkCredential _networkCredential;
		private readonly WorkspaceInfo _workspaceInfo;

		public TfsFileSync(string filename)
		{
			_filename = filename;
			_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(filename);
			AssertWorkspace();

			_networkCredential = new NetworkCredential("123", "123", "123");
		}

		private void AssertWorkspace()
		{
			if (_workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS Manager: No workspace is available or defined for the path " + _filename);
		}

		private bool FileExistsOnServer()
		{
			bool fileExistsInTfs;

			using (var collection = new TfsTeamProjectCollection(_workspaceInfo.ServerUri, _networkCredential))
			{
				try
				{
					collection.EnsureAuthenticated();

					var versionControlServer = (VersionControlServer) collection.GetService(typeof (VersionControlServer));
					versionControlServer.NonFatalError += OnNonFatalError;

					var workspace = versionControlServer.GetWorkspace(_workspaceInfo);
					var serverFilePath = workspace.GetServerItemForLocalItem(_filename);
					fileExistsInTfs = versionControlServer.ServerItemExists(serverFilePath, ItemType.File);
				}
				catch (Exception ex)
				{
					Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Could not communicate with TFS Server for " + _filename, ex, this);
					throw;
				}
			}

			return fileExistsInTfs;
		}

		public bool CheckoutFile()
		{
			bool fileExistsOnFileSystem = File.Exists(_filename);
			if (!fileExistsOnFileSystem)
			{
				throw new Exception("[Rainbow] TFS Manager: Cannot checkout a file that does not exist on disk for " + _filename);
			}

			bool fileExistsOnServer = FileExistsOnServer();
			if (!fileExistsOnServer)
			{
				throw new Exception("[Rainbow] TFS Manager: Cannot checkout a file that does not exist in TFS for " + _filename);
			}

			bool updateSuccess;

			using (var collection = new TfsTeamProjectCollection(_workspaceInfo.ServerUri, _networkCredential))
			{
				try
				{
					collection.EnsureAuthenticated();

					var versionControlServer = (VersionControlServer)collection.GetService(typeof(VersionControlServer));
					versionControlServer.NonFatalError += OnNonFatalError;

					var workspace = versionControlServer.GetWorkspace(_workspaceInfo);

					var updateResult = workspace.PendEdit(_filename);
					updateSuccess = updateResult == 1;

					if (updateSuccess == false)
					{
						var message = string.Format("TFS checkout was unsuccessful for {0}", _filename);
						throw new Exception(message);
					}
				}
				catch (Exception ex)
				{
					Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Could not checkout file in TFS for " + _filename, ex, this);
					throw;
				}

				return updateSuccess;
			}
		}

		public bool AddFile()
		{
			bool fileExistsOnFileSystem = File.Exists(_filename);
			if (!fileExistsOnFileSystem)
			{
				throw new Exception("[Rainbow] TFS Manager: Cannot add a file that does not exist on disk for " + _filename);
			}

			bool fileExistsOnServer = FileExistsOnServer();
			if (fileExistsOnServer)
			{
				throw new Exception("[Rainbow] TFS Manager: Cannot add a file that already exists in TFS for " + _filename);
			}

			bool addSuccess;

			using (var collection = new TfsTeamProjectCollection(_workspaceInfo.ServerUri, _networkCredential))
			{
				try
				{
					collection.EnsureAuthenticated();

					var versionControlServer = (VersionControlServer)collection.GetService(typeof(VersionControlServer));
					versionControlServer.NonFatalError += OnNonFatalError;

					var workspace = versionControlServer.GetWorkspace(_workspaceInfo);

					var updateResult = workspace.PendAdd(_filename);
					addSuccess = updateResult == 1;

					if (addSuccess == false)
					{
						var message = string.Format("TFS add was unsuccessful for {0}", _filename);
						throw new Exception(message);
					}
				}
				catch (Exception ex)
				{
					Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Could not add file to TFS for " + _filename, ex, this);
					throw;
				}

				return addSuccess;
			}
		}

		private void OnNonFatalError(Object sender, ExceptionEventArgs e)
		{
			var message = e.Exception != null ? e.Exception.Message : e.Failure.Message;
			Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Non-fatal exception: " + message, this);
		}
	}
}