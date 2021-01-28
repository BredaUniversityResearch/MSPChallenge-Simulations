using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Newtonsoft.Json;

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
			APIRequest.Perform(serverBaseAddress, "/api/simulations/GetWatchdogTokenForServer", null, null,
				out WatchdogTokenResponse response);
			return response.watchdog_token;
		}
	}
}
