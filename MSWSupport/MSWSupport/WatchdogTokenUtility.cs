using System;
using System.Diagnostics.CodeAnalysis;

namespace MSWSupport
{
	public class WatchdogTokenUtility
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		class WatchdogTokenResponse
		{
			public string watchdog_token = null;
		};

		public static string GetWatchdogTokenForServerAtAddress(string serverBaseAddress)
		{
			try
			{
				if (APIRequest.Perform(serverBaseAddress, "/api/simulation/GetWatchdogTokenForServer", null, null,
					out WatchdogTokenResponse response))
				{
					return response.watchdog_token;
				}
			}
			catch (ApiUnauthorizedWebException ex)
			{
				return String.Empty;
			}
			return String.Empty;
		}
	}
}
