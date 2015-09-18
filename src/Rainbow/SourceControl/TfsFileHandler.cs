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
		private readonly string _filename;

		public virtual bool FileExistsOnServer { get; private set; }
		public virtual bool FileExistsOnFileSystem { get { return File.Exists(_filename); } }

		public TfsFileHandler(TfsTeamProjectCollection tfsTeamProjectCollection, string filename)
		{
			_tfsTeamProjectCollection = tfsTeamProjectCollection;
			_filename = filename;

			FileExistsOnServer = GetFileExistsOnServer();
		}

		private WorkspaceInfo GetLocalWorkspaceInfo()
		{
			var workspace = Workstation.Current.GetLocalWorkspaceInfo(_filename);
			AssertWorkspace(workspace);

			return workspace;
		}

		private void AssertWorkspace(WorkspaceInfo workspaceInfo)
		{
			if (workspaceInfo != null) return;
			throw new Exception("[Rainbow] TFS File Handler: No workspace is available or defined for the path " + _filename);
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

		private Workspace GetWorkspace()
		{
			var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
			versionControlServer.NonFatalError += OnNonFatalError;

			return GetWorkspace(versionControlServer);
		}

		private Workspace GetWorkspace(VersionControlServer versionControlServer)
		{
			var workspaceInfo = GetLocalWorkspaceInfo();
			var workspace = versionControlServer.GetWorkspace(workspaceInfo);
			return workspace;
		}

		protected virtual bool GetFileExistsOnServer()
		{
			bool fileExistsInTfs;

			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var workspace = GetWorkspace(versionControlServer);
				var serverFilePath = workspace.GetServerItemForLocalItem(_filename);
				fileExistsInTfs = versionControlServer.ServerItemExists(serverFilePath, ItemType.Any);
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
		protected virtual void UndoNonMatchingPendingChanges(ChangeType changeType)
		{
			try
			{
				var workspace = GetWorkspace();

				// get pending changes that differ from changeType
				var changes = workspace.GetPendingChanges(_filename, RecursionType.None, false);
				var change = changes.FirstOrDefault(c => c.ChangeType != changeType);
				if (change == null) return;

				// update our workspace and refresh the local copy
				bool writeToDisk = !FileExistsOnFileSystem;
				workspace.Undo(new[] { _filename }, writeToDisk);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not revert pending change for " + _filename, ex, this);
				throw;
			}
		}

		protected virtual bool HasPendingChanges(ChangeType changeType)
		{
			bool hasRequestedChange;

			try
			{
				var workspace = GetWorkspace();
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

		private void TryRefreshLocalWithTfs()
		{
			try
			{
				var versionControlServer = (VersionControlServer)_tfsTeamProjectCollection.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += OnNonFatalError;

				var item = versionControlServer.GetItem(_filename, VersionSpec.Latest, DeletedState.Any, GetItemsOptions.Download);
				item.DownloadFile(_filename);

				var workspace = GetWorkspace(versionControlServer);
				workspace.Get(new[] { _filename }, VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not refresh local from TFS " + _filename, ex, this);
				throw;
			}
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

			if (HasPendingChanges(ChangeType.Delete)) return true;

			// revert any conflicting TFS pending changes that prevent us from submitting a pending delete
			UndoNonMatchingPendingChanges(ChangeType.Delete);

			var updateResult = DeleteFileInTfs();
			return updateResult == 1;
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

			AssertFileExistsOnFileSystem();

			var updateResult = EditFileInTfs();
			return updateResult == 1;
		}

		public bool AddFile()
		{
			AssertFileExistsOnFileSystem();
			AssertFileDoesNotExistInTfs();

			var updateResult = AddFileToTfs();
			return updateResult == 1;
		}

		protected virtual int AddFileToTfs()
		{
			try
			{
				var workspace = GetWorkspace();
				return workspace.PendAdd(_filename);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not add file to TFS for " + _filename, ex, this);
				throw;
			}
		}

		protected virtual int EditFileInTfs()
		{
			try
			{
				var workspace = GetWorkspace();
				return workspace.PendEdit(_filename);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not checkout file in TFS for " + _filename, ex, this);
				throw;
			}
		}

		protected virtual int DeleteFileInTfs()
		{
			try
			{
				var workspace = GetWorkspace();
				return workspace.PendDelete(_filename);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Could not checkout file in TFS for " + _filename, ex, this);
				throw;
			}
		}

		private void OnNonFatalError(Object sender, ExceptionEventArgs e)
		{
			var message = e.Exception != null ? e.Exception.Message : e.Failure.Message;
			Sitecore.Diagnostics.Log.Error("[Rainbow] TFS File Handler: Non-fatal exception: " + message, this);
		}
	}
}