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
		private static readonly Lazy<Type> ReflectedTypeEnforcedVersionPresenceDisabler = new Lazy<Type>(() =>
		{
			// Lifted out of SPE codebase - cheers Adam :)
			var kernel = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName()
				.Name.Equals("Sitecore.Kernel", StringComparison.OrdinalIgnoreCase));

			Type reflectedType = null;

			if (kernel != null)
			{
				reflectedType = kernel.GetType("Sitecore.Data.Items.EnforceVersionPresenceDisabler", false, true);
			}

			return reflectedType;
		});

		public VersionSafeEnforceVersionPresenceDisabler()
		{
			if(ReflectedTypeEnforcedVersionPresenceDisabler.Value != null)
				_sitecoreEnforceVersionPresenceDisabler = (IDisposable)ReflectionUtil.CreateObject(ReflectedTypeEnforcedVersionPresenceDisabler.Value);
		}

		public void Dispose()
		{
			_sitecoreEnforceVersionPresenceDisabler?.Dispose();
		}
	}
}