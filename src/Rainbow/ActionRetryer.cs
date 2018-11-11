using System;
using System.Threading;

namespace Rainbow
{
	public static class ActionRetryer
	{
		public static void Perform(Action action, int delayInMs = 500)
		{
			const int retries = 3;
			for (int i = 0; i < retries; i++)
			{
				try
				{
					action();
				}
				catch (Exception)
				{
					// we wait 500ms and retry up to 3x before rethrowing
					if (i < retries - 1)
					{
						Thread.Sleep(delayInMs);
						continue;
					}

					throw;
				}

				break;
			}
		}
	}
}
