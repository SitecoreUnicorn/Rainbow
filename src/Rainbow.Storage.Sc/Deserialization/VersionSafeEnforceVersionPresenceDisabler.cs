using System;
using System.Linq;
using Sitecore.Reflection;

namespace Rainbow.Storage.Sc.Deserialization
{
	/// <summary>
	/// This class acts as a Sitecore Version-safe facade to the EnforceVersionPresenceDisabler functionality of later Sitecore versions
	/// </summary>
	public class VersionSafeEnforceVersionPresenceDisabler : IDisposable
	{
		private readonly IDisposable _sitecoreEnforceVersionPresenceDisabler;

		public VersionSafeEnforceVersionPresenceDisabler()
		{
			_sitecoreEnforceVersionPresenceDisabler = null;

			// Lifted out of SPE codebase - cheers Adam :)
			var kernel = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName()
				.Name.Equals("Sitecore.Kernel", StringComparison.OrdinalIgnoreCase));

			if (kernel != null)
			{
				var enforceVersionPresenceDisabler = kernel.GetType("Sitecore.Data.Items.EnforceVersionPresenceDisabler", false, true);
				if (enforceVersionPresenceDisabler != null)
				{
					_sitecoreEnforceVersionPresenceDisabler = (IDisposable) ReflectionUtil.CreateObject(enforceVersionPresenceDisabler);
				}
			}
		}

		public void Dispose()
		{
			_sitecoreEnforceVersionPresenceDisabler?.Dispose();
		}
	}
}