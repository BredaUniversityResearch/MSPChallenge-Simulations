using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSWSupport
{
	public static class APIRequest
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		public class ApiResponseWrapper
		{
			public bool success = false;
			public string message = null;
			public JToken payload = null;
		};

		public static bool Perform<TTargetType>(string serverUrl, string apiUrl, string currentAccessToken, NameValueCollection postValues, out TTargetType result, JsonSerializer jsonSerializer = null)
		{
			bool success = Perform(serverUrl, apiUrl, currentAccessToken, postValues, out JToken jsonResult);
			if (success)
			{
				if (jsonResult != null)
				{
					if (jsonSerializer != null)
					{
						result = jsonResult.ToObject<TTargetType>(jsonSerializer);
					}
					else
					{
						result = jsonResult.ToObject<TTargetType>();
					}
				}
				else
				{
					result = default;
				}
			}
			else
			{
				result = default;
			}

			return success;
		}

		public static bool Perform(string serverUrl, string apiUrl, string currentAccessToken,
			NameValueCollection postValues)
		{
			bool success = Perform(serverUrl, apiUrl, currentAccessToken, postValues, out JToken jsonResult, null);
			if (success)
			{
				if (jsonResult != null && jsonResult.Type != JTokenType.Null)
				{
					Console.WriteLine($"ApiRequest::Perform for {serverUrl}/{apiUrl} got response when none was expected. Response: {jsonResult}");
					success = false;
				}
			}

			return success;
		}

		private static bool Perform(string serverUrl, string apiUrl, string currentAccessToken,
			NameValueCollection postValues, out JToken responsePayload)
		{
			string fullServerUrl = $"{serverUrl}/{apiUrl}";
			if (postValues == null) postValues = new NameValueCollection();
			string response = null;
			try
			{
				response = HttpGet(fullServerUrl, currentAccessToken, postValues);
			}
			catch (WebException ex)
			{
				Console.WriteLine($"ApiRequest::Perform for {fullServerUrl} failed with exception: {ex.Message}");
				responsePayload = null;
				return false;
			}

			if (string.IsNullOrEmpty(response))
			{
				responsePayload = null;
				return false;
			}

			ApiResponseWrapper wrapper = DeserializeJson<ApiResponseWrapper>(response);
			if (wrapper == null || wrapper.success == false)
			{
				responsePayload = null;
				Console.WriteLine($"ApiRequest::Perform for {fullServerUrl} failed: {((wrapper != null)? wrapper.message : "JSON Decode Failed")}");
				return false;
			}

			responsePayload = wrapper.payload;
			return true;
		}

		private static TOutputType DeserializeJson<TOutputType>(string jsonData)
		{
			try
			{
				return JsonConvert.DeserializeObject<TOutputType>(jsonData);
			}
			catch (JsonReaderException ex)
			{
				Console.WriteLine("Error deserializing JSON String.");
				Console.WriteLine("Exception: " + ex.Message);
				Console.WriteLine("InputData: ");
				Console.WriteLine(jsonData);
				return default;
			}
		}

		private static string HttpGet(string fullApiUrl, string currentAccessToken, NameValueCollection values)
		{
			WebClient webclient = new WebClient();
			webclient.Headers.Add(MSWConstants.APITokenHeader, currentAccessToken);
			byte[] response = webclient.UploadValues(fullApiUrl, values);

			return System.Text.Encoding.UTF8.GetString(response);
		}
	}
}
