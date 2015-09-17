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

		public bool FileExistsOnServer { get; private set; }
		public bool FileExistsOnFileSystem { get; private set; }

		public TfsFileHandler(TfsTeamProjectCollection tfsTeamProjectCollection, string filename)
		{
			_tfsTeamProjectCollection = tfsTeamProjectCollection;
			_filename = filename;

			_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(filename);
			AssertWorkspace(_workspaceInfo, filename);

			FileExistsOnFileSystem = GetFileExistsOnFileSystem();
			FileExistsOnServer = GetFileExistsOnServer();
		}

		private void AssertWorkspace(WorkspaceInfo workspaceInfo, string filename)
		{
			if (workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS File Handler: No workspace is available or defined for the path " + filename);
		}

		private void AssertFileExistsOnFileSystem()
		{
			if (!FileExistsOnFileSystem)
			{
				throw new Exception("[Rainbow] TFS File Handler: file does not exist on disk for " + _filename);
			}
		}

		private void AssertFileExistsInTfs()
		{
			if (!FileExistsOnServer)
			{
				throw new Exception("[Rainbow] TFS File Handler: file does not exist in TFS for " + _filename);
			}
		}

		private void AssertFileDoesNotExistInTfs()
		{
			if (FileExistsOnServer)
			{
				throw new Exception("[Rainbow] TFS File Handler: file exists in TFS for " + _filename);
			}
		}

		private bool GetFileExistsOnFileSystem()
		{
			return File.Exists(_filename);
		}

		private bool GetFileExistsOnServer()
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
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not communicate with TFS Server for " + _filename, ex, this);
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

				// cannot undo a pending change of a file in conflict
				var conflicts = workspace.QueryConflicts(new[] { _filename }, false);
				bool hasConflicts = conflicts.Any();
				if (hasConflicts)
				{

				}

				// undo workspace changes
				var changes = workspace.GetPendingChanges(_filename, RecursionType.None, false);
				var change = changes.FirstOrDefault(c => c.ChangeType != changeType);
				if (change != null)
				{
					bool writeToDisk = !FileExistsOnFileSystem;
					workspace.Undo(new[] { _filename }, writeToDisk);
					
					conflicts = workspace.QueryConflicts(new[] { _filename }, writeToDisk);
					hasConflicts = conflicts.Any();
					if (hasConflicts)
					{

					}


					//workspace.Get(new[] {_filename}, VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);

					//foreach (var conflict in conflicts)
					//{
					//	workspace.ResolveConflict(conflict);
					//}


					//workspace.Get(_filename, VersionSpec.

					//// Grab a copy of the file from TFS and overwrite our local to keep TFS from complaining about it not being downloaded
					//// If not downloaded, TFS won't allow any additional pending edits and stuff breaks.
					//var serverCopy = versionControlServer.GetItem(change.ItemId, change.Version, GetItemsOptions.Download);
					//serverCopy.DownloadFile(_filename);
				}
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not revert pending change for " + _filename, ex, this);
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
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not communicate with TFS Server for " + _filename, ex, this);
				throw;
			}

			return hasRequestedChange;
		}

		public bool CheckoutFileForDelete()
		{
			// if the file doesn't exist on the local filesystem, we're out of sync. Record in logs and allow to pass through.
			if (!FileExistsOnFileSystem)
			{
				Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS File Handler: Attempting to delete a file that doesn't exist on the local filesystem for " + _filename, this);
			}

			// if the file doesn't exist on the TFS server, we're out of sync. Allow the local deletion.
			if (!FileExistsOnServer)
			{
				Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS File Handler: Attempting to delete a file that doesn't exist on the server for " + _filename, this);
				return true;
			}

			// if the file is already under edit, no need to checkout again
			return DeleteFile();
		}

		private void TryRefreshLocalWithTfs()
		{
			try
			{
				var versionControlServer = (VersionControlServer) _tfsTeamProjectCollection.GetService(typeof (VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = versionControlServer.GetWorkspace(_workspaceInfo);
				workspace.Get(new[] {_filename}, VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not refresh local from TFS " + _filename, ex, this);
				throw;
			}
		}

		public bool CheckoutFileForEdit()
		{
			AssertFileExistsInTfs();

			// if the file is already under edit, no need to checkout again
			if (HasPendingChanges(ChangeType.Edit)) return true;

			// revert any conflicting TFS pending changes that prevent us from submitting a pending edit
			UndoNonMatchingPendingChanges(ChangeType.Edit);

			// if we're out of sync, pull down from TFS to keep it from complaining on edit
			if (FileExistsOnServer && !FileExistsOnFileSystem)
			{
				TryRefreshLocalWithTfs();
			}

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
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not checkout file in TFS for " + _filename, ex, this);
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
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not add file to TFS for " + _filename, ex, this);
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
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not checkout file in TFS for " + _filename, ex, this);
				throw;
			}

			return true;
		}

		private void OnNonFatalError(Object sender, ExceptionEventArgs e)
		{
			var message = e.Exception != null ? e.Exception.Message : e.Failure.Message;
			Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Non-fatal exception: " + message, this);
		}

		//private void AutoResolveConflistKeepLocal(Workspace workspace)
		//{
		//	// auto-resolve conflict by trusting local copy
		//	var conflicts = workspace.QueryConflicts(new [] {_filename}, false);
		//	foreach (var conflict in conflicts)
		//	{
		//		workspace.ResolveConflict(conflict);
		//	}
		//}
	}
}