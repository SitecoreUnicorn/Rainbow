using System;
using System.Web;
using Sitecore.Configuration;

namespace Rainbow.SourceControl
{
	public class SourceControlManager : ISourceControlManager
	{
		private string _username;
		private string _password;
		private string _domain;
		private string _sourceControlProvider;
		private readonly ISourceControlSync _sourceControlSync;

		private const string UsernameKey = "Rainbow.SCM.Login";
		private const string PasswordKey = "Rainbow.SCM.Password";
		private const string DomainKey = "Rainbow.SCM.Domain";
		private const string SourceControlProviderKey = "Rainbow.SCM.SourceControlProvider";

		protected string Username
		{
			get
			{
				if (!string.IsNullOrEmpty(_username)) return _username;

				var configSetting = Settings.GetSetting(UsernameKey);
				_username = configSetting;

				return _username;
			}
		}

		protected string Password
		{
			get
			{
				if (!string.IsNullOrEmpty(_password)) return _password;

				var configSetting = Settings.GetSetting(PasswordKey);
				_password = configSetting;

				return _password;
			}
		}

		protected string Domain
		{
			get
			{
				if (!string.IsNullOrEmpty(_domain)) return _domain;

				var configSetting = Settings.GetSetting(DomainKey);
				_domain = configSetting;

				return _domain;
			}
		}

		protected string SourceControlProvider
		{
			get
			{
				if (!string.IsNullOrEmpty(_sourceControlProvider)) return _sourceControlProvider;

				var configSetting = Settings.GetSetting(SourceControlProviderKey);
				_sourceControlProvider = configSetting;

				return _sourceControlProvider;
			}
		}

		public SourceControlManager()
		{
			_sourceControlSync = GetSourceControlSyncInstance();
		}

		private ISourceControlSync GetSourceControlSyncInstance()
		{
			var type = Type.GetType(SourceControlProvider);
			if (type == null)
			{
				throw new Exception(string.Format("Cannot resolve type {0} from setting {1}", SourceControlProvider, SourceControlProviderKey));
			}

			var sourceControlSync = Activator.CreateInstance(type, GetSettings()) as ISourceControlSync;
			if (sourceControlSync == null)
			{
				throw new Exception(string.Format("Cannot create instance of type {0}", SourceControlProvider));
			}

			return sourceControlSync;
		}

		private ScmSettings GetSettings()
		{
			return new ScmSettings()
			{
				Domain = Domain,
				Password = Password,
				Username = Username,
				ApplicationRootPath = HttpContext.Current.Server.MapPath("/")
			};
		}

		public bool EditPreProcessing(string filename)
		{
			return _sourceControlSync.EditPreProcessing(filename);
		}

		public bool EditPostProcessing(string filename)
		{
			return _sourceControlSync.EditPostProcessing(filename);
		}

		public bool DeletePreProcessing(string filename)
		{
			return _sourceControlSync.DeletePreProcessing(filename);
		}
	}
}