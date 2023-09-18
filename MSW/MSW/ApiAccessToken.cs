using System;
using Newtonsoft.Json;

namespace MSW
{
	public class ApiAccessToken
	{
		[JsonProperty]
		private string token = "";
		[JsonProperty]
		private DateTime valid_until = new DateTime(0);

		public string GetTokenAsString()
		{
			return token;
		}
		
		public void SetToken(string a_token)
		{
			token = a_token;
		}
	};
}