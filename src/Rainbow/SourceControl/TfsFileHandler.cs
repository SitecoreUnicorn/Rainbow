using System;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Rainbow.SourceControl
{
	public class TfsFileHandler
	{
		private readonly TfsTeamProjectCollection _tfsTeamProjectCollection;
		private readonly WorkspaceInfo _workspaceInfo;
		private readonly string _filename;

		public TfsFileHandler(TfsTeamProjectCollection tfsTeamProjectCollection, string filename)
		{
			_tfsTeamProjectCollection = tfsTeamProjectCollection;
			_filename = filename;

			_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(filename);
			AssertWorkspace(_workspaceInfo, filename);
		}

		private void AssertWorkspace(WorkspaceInfo workspaceInfo, string filename)
		{
			if (workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS Manager: No workspace is available or defined for the path " + filename);
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
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
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

		/// <summary>
		/// Undo any pending change that does not match the action we're performing. For example, if we request an edit and the pending change
		/// is delete, undo the delete so we can place the item in pending edit. Without the undo, an exceptionw would be thrown.
		/// </summary>
		/// <param name="changeType">Requested change</param>
		private void UndoNonMatchingPendingChanges(ChangeType changeType)
		{
			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);
				var changes = workspace.GetPendingChanges(_filename, RecursionType.None, false);

				var change = changes.FirstOrDefault(c => c.ChangeType != changeType);
				if (change != null)
				{
					workspace.Undo(_filename);

					// Grab a copy of the file from TFS and overwrite our local to keep TFS from complaining about it not being downloaded
					// If not downloaded, TFS won't allow any additional pending edits and stuff breaks
					var serverCopy = versionControlServer.GetItem(change.ItemId, change.Version, GetItemsOptions.Download);
					serverCopy.DownloadFile(_filename);
				}
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Could not revert pending change for " + _filename, ex, this);
				throw;
			}
		}

		private bool HasPendingChanges(ChangeType changeType)
		{
			bool hasRequestedChange;

			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);
				var changes = workspace.GetPendingChanges(_filename, RecursionType.None, false);
				
				hasRequestedChange = changes.Any(c => c.ChangeType == changeType);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Could not communicate with TFS Server for " + _filename, ex, this);
				throw;
			}

			return hasRequestedChange;
		}

		public bool CheckoutFileForEdit()
		{
			bool fileExistsOnTfsServer = FileExistsOnServer();
			return fileExistsOnTfsServer ? EditFile() : AddFile();
		}

		public bool CheckoutFileForDelete()
		{
			// if the file doesn't exist on the local filesystem, we're out of sync. Record in logs and allow to pass through.
			bool fileExistsOnFileSystem = File.Exists(_filename);
			if (!fileExistsOnFileSystem)
			{
				Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS Manager: Attempting to delete a file that doesn't exist on the local filesystem for " + _filename, this);
			}

			// if the file doesn't exist on the TFS server, we're out of sync. Allow the local deletion.
			bool fileExistsOnServer = FileExistsOnServer();
			if (!fileExistsOnServer)
			{
				Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS Manager: Attempting to delete a file that doesn't exist on the server for " + _filename, this);
				return true;
			}

			// if the file is already under edit, no need to checkout again
			return DeleteFile();
		}

		private bool EditFile()
		{
			AssertFileExistsOnFileSystem();
			AssertFileExistsInTfs();

			// if the file is already under edit, no need to checkout again
			if (HasPendingChanges(ChangeType.Edit)) return true;

			// revert any conflicting TFS pending changes that prevent us from submitting a pending edit
			UndoNonMatchingPendingChanges(ChangeType.Edit);

			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
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

		private bool AddFile()
		{
			AssertFileExistsOnFileSystem();
			AssertFileDoesNotExistInTfs();

			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
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

		private bool DeleteFile()
		{
			if (HasPendingChanges(ChangeType.Delete)) return true;

			// revert any conflicting TFS pending changes that prevent us from submitting a pending delete
			UndoNonMatchingPendingChanges(ChangeType.Delete);

			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);
				var updateResult = workspace.PendDelete(_filename);
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

		private void OnNonFatalError(Object sender, ExceptionEventArgs e)
		{
			var message = e.Exception != null ? e.Exception.Message : e.Failure.Message;
			Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Manager: Non-fatal exception: " + message, this);
		}
	}
}