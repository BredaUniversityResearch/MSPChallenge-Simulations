using System.Diagnostics.CodeAnalysis;

namespace MSW
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	class ApiUserRequestTokenResponse
	{
		public string api_access_token = null;
		public string api_refresh_token = null;
	}
}
