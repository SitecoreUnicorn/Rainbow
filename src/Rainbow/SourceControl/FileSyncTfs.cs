using System;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Rainbow.SourceControl
{
	public class FileSyncTfs : ISourceControlSync
	{
		private readonly string _filename;
		private readonly NetworkCredential _networkCredential;
		private readonly WorkspaceInfo _workspaceInfo;

		protected TfsConnection TfsTeamProjectCollection
		{
			get
			{
				var connection = TfsPersistentConnection.Instance(_workspaceInfo.ServerUri, _networkCredential);
				return connection.TfsTeamProjectCollection;
			}
		}

		public FileSyncTfs(ScmSettings settings)
		{
			_filename = settings.Filename;
			_workspaceInfo = GetWorkspaceInfo();
			AssertWorkspace();

			_networkCredential = new NetworkCredential(settings.Username, settings.Password, settings.Domain);
		}

		protected virtual WorkspaceInfo GetWorkspaceInfo()
		{
			return Workstation.Current.GetLocalWorkspaceInfo(_filename);
		}

		private void AssertWorkspace()
		{
			if (_workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS Manager: No workspace is available or defined for the path " + _filename);
		}

		private void AssertFileExistsOnFileSystem()
		{
			bool fileExistsOnFileSystem = FileExistsOnFileSystem();
			if (!fileExistsOnFileSystem)
			{
				throw new Exception("[Rainbow] TFS Manager: file does not exist on disk for " + _filename);
			}
		}

		private void AssertFileExistsInTfs()
		{
			bool fileExistsOnServer = FileExistsOnServer();
			if (!fileExistsOnServer)
			{
				throw new Exception("[Rainbow] TFS Manager: file does not exist in TFS for " + _filename);
			}
		}

		private void AssertFileDoesNotExistInTfs()
		{
			bool fileExistsOnServer = FileExistsOnServer();
			if (fileExistsOnServer)
			{
				throw new Exception("[Rainbow] TFS Manager: file exists in TFS for " + _filename);
			}
		}

		protected virtual bool FileExistsOnFileSystem()
		{
			return File.Exists(_filename);
		}

		private bool FileExistsOnServer()
		{
			bool fileExistsInTfs;

			try
			{
				var versionControlServer = (VersionControlServer)TfsTeamProjectCollection.GetService(typeof(VersionControlServer));
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

			return fileExistsInTfs;
		}

		private bool HasPendingChanges()
		{
			bool hasChanges;

			try
			{
				var versionControlServer = (VersionControlServer)TfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);
				var changes = workspace.GetPendingChanges(_filename, RecursionType.None, false);
				hasChanges = changes.Any();
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Could not communicate with TFS Server for " + _filename, ex, this);
				throw;
			}

			return hasChanges;
		}

		public bool CheckoutFileForDelete()
		{
			// if the file doesn't exist on the local filesystem, we're out of sync. Record in logs and allow to pass through.
			bool fileExistsOnFileSystem = File.Exists(_filename);
			if (!fileExistsOnFileSystem)
			{
				Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS Manager: Attempting to delete a file that doesn't exist on the local filesystem for " + _filename, this);
			}

			// if the file doesn't exist on the TFS server, we're out of sync. Allow the deletion.
			bool fileExistsOnServer = FileExistsOnServer();
			if (!fileExistsOnServer)
			{
				Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS Manager: Attempting to delete a file that doesn't exist on the server for " + _filename, this);
				return true;
			}

			// if the file is already under edit, no need to checkout again
			return HasPendingChanges() || CheckoutFile();
		}

		public bool CheckoutFileForEdit()
		{
			AssertFileExistsOnFileSystem();

			// if the file is already under edit, no need to checkout again
			if (HasPendingChanges()) return true;

			AssertFileExistsInTfs();
			
			return CheckoutFile();
		}

		private bool CheckoutFile()
		{
			try
			{
				var versionControlServer = (VersionControlServer)TfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);

				var updateResult = workspace.PendEdit(_filename);
				var updateSuccess = updateResult == 1;
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

			return true;
		}

		public bool AddFile()
		{
			AssertFileExistsOnFileSystem();
			AssertFileDoesNotExistInTfs();

			try
			{
				var versionControlServer = (VersionControlServer)TfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);

				var updateResult = workspace.PendAdd(_filename);
				var addSuccess = updateResult == 1;
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

			return true;
		}

		private void OnNonFatalError(Object sender, ExceptionEventArgs e)
		{
			var message = e.Exception != null ? e.Exception.Message : e.Failure.Message;
			Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Non-fatal exception: " + message, this);
		}
	}
}