using System;
using System.Net;
using Microsoft.TeamFoundation.Client;

namespace Rainbow.SourceControl
{
	/// <summary>
	/// Attempts to establish a connection with the TFS server in a singleton scope. If an instance exists, first verify 
	/// we're still authenticated. On failure of authentication verification, attempt to re-establish a connection.
	/// </summary>
	public sealed class TfsPersistentConnection
	{
		public TfsTeamProjectCollection TfsTeamProjectCollection { get; private set; }
		private static volatile TfsPersistentConnection _instance;
		private static readonly object SyncRoot = new Object();

		private TfsPersistentConnection(TfsTeamProjectCollection tfsTeamProjectCollection)
		{
			TfsTeamProjectCollection = tfsTeamProjectCollection;
		}

		/// <summary>
		/// Obtain an instance of the TFS connection. Verifies connection is still established with TFS.
		/// </summary>
		/// <param name="uri">TFS server URI</param>
		/// <param name="credentials">TFS server credentials</param>
		/// <returns></returns>
		public static TfsPersistentConnection Instance(Uri uri, ICredentials credentials)
		{
			bool isValidInstance = _instance != null && _instance.TfsTeamProjectCollection != null;
			if (isValidInstance)
			{
				try
				{
					_instance.TfsTeamProjectCollection.EnsureAuthenticated();
					return _instance;
				}
				catch (Exception ex)
				{
					Sitecore.Diagnostics.Log.Warn("[Rainbow] TFS Persistent Connection: could not ensure connection with TFS server. Attempting to re-establish the connection.", ex, credentials);
				}
			}

			EstablishTfsPersistentConnection(uri, credentials);

			return _instance;
		}

		/// <summary>
		/// Generates a new connection to TFS and verifies the connection has been established.
		/// </summary>
		/// <param name="uri">TFS server URI</param>
		/// <param name="credentials">TFS server credentials</param>
		private static void EstablishTfsPersistentConnection(Uri uri, ICredentials credentials)
		{
			lock (SyncRoot)
			{
				try
				{
					var connection = new TfsTeamProjectCollection(uri, credentials);
					_instance = new TfsPersistentConnection(connection);
					_instance.TfsTeamProjectCollection.EnsureAuthenticated();
				}
				catch (Exception ex)
				{
					Sitecore.Diagnostics.Log.Error("[Rainbow] TFS Persistent Connection: could not establish connection with TFS server.", ex, credentials);
					throw;
				}
			}
		}
	}
}