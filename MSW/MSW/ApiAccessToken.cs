using System;
using Newtonsoft.Json;

namespace MSW
{
	public class ApiAccessToken
	{
		[JsonProperty]
		private readonly long token = 0;
		[JsonProperty]
		private readonly DateTime valid_until = new DateTime(0);

		public string GetTokenAsString()
		{
			return token.ToString();
		}
	};
}