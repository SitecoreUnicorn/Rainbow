using System.Linq;
using System.Reflection;

namespace Rainbow
{
	public static class RainbowVersion
	{
		public static string Current => ((AssemblyInformationalVersionAttribute) Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).Single()).InformationalVersion;
	}
}