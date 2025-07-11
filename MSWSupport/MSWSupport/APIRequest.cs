using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSWSupport
{
	public static class APIRequest
	{
		private const int DEFAULT_API_UNAUTHORIZED_SLEEP_SEC = 4;
		private const string DEFAULT_API_UNAUTHORIZED_MESSAGE_FORMAT = "API refused access... Waiting {0} sec and retrying";

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		public class ApiResponseWrapper
		{
			public bool success = false;
			public string message = null;
			public JToken payload = null;
		};

		public static bool SleepOnApiUnauthorizedWebException(Action action, int sleepSec = DEFAULT_API_UNAUTHORIZED_SLEEP_SEC,
			string messageFormat = DEFAULT_API_UNAUTHORIZED_MESSAGE_FORMAT)
		{
	        try
	        {
	            action();
	        }
	        catch (ApiUnauthorizedWebException ex)
	        {
				Console.WriteLine(messageFormat, sleepSec);
				Thread.Sleep(sleepSec * 1000);
				return true; // we are sleeping
	        }
	        return false;
		}

		public static bool Perform<TTargetType>(
			string serverUrl,
			string apiUrl,
			out TTargetType result,
			string? currentAccessToken = null,
			NameValueCollection? postValues = null,
			JsonSerializer? jsonSerializer = null,
			bool logServerResponseLogs = false
		) {
			// do not use named parameters here, as it will break the overload resolution
			bool success = PerformInternal(
				serverUrl, apiUrl, out JToken? jsonResult, currentAccessToken, postValues, logServerResponseLogs
			);
			if (success)
			{
				if (jsonResult != null)
				{
					result = jsonSerializer != null ?
						jsonResult.ToObject<TTargetType>(jsonSerializer) : jsonResult.ToObject<TTargetType>();
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

		public static bool Perform(
			string serverUrl,
			string apiUrl,
			string? currentAccessToken = null,
			NameValueCollection? postValues = null,
			bool logServerResponseLogs = false
		) {
			bool success = Perform(
				serverUrl,
				apiUrl,
				out JToken jsonResult,
				currentAccessToken,
				postValues,
				logServerResponseLogs: logServerResponseLogs
			);
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

		private static bool PerformInternal(
			string serverUrl,
			string apiUrl,
			out JToken? responsePayload,
			string? currentAccessToken = null,
			NameValueCollection? postValues = null,
			bool logServerResponseLogs = false
		) {
			string fullServerUrl = $"{serverUrl}{apiUrl}";
			string response = null;
			try
			{
				response = HttpGet(fullServerUrl, currentAccessToken, postValues);
			}
			catch (WebException ex)
			{
				if (null != ex.Response &&
					((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Unauthorized)
				{
					throw new ApiUnauthorizedWebException(ex); // allow child code to handle this one
				}
				if (null != ex.Response &&
					((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Gone)
				{
					throw new SessionApiGoneWebException(ex); // allow child code to handle this one
				}

				ConsoleLogger.Error($"ApiRequest::Perform for {fullServerUrl} failed with exception: {ex.Message}");
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
				ConsoleLogger.Error($"ApiRequest::Perform for {fullServerUrl} failed: {((wrapper != null)? wrapper.message : "JSON Decode Failed")}");
				return false;
			}

			responsePayload = wrapper.payload;
			if (!logServerResponseLogs) return true;
			if (wrapper.payload is not { Type: JTokenType.Object }) return true;
			var payloadObj = wrapper.payload.ToObject<JObject>();
			var logs = payloadObj?.GetValue("logs");
			var logsArray = (logs as JArray)?.ToObject<string[]>();
			if (logsArray == null || logsArray.Length == 0) return true;
			ConsoleLogger.Info(
				"Server response logs for " + fullServerUrl +
					(
						postValues.Count  == 0 ? "" : " with post values\n" +
						    JsonConvert.SerializeObject(
							    postValues.AllKeys.Take(5).ToDictionary(
								    k => k,
								    k => postValues[k]?.Substring(0, Math.Min(30, postValues[k]?.Length ?? 0)) +
								         (postValues[k]?.Length > 30 ? "..." : "")
								),
							    Formatting.Indented
							) + ":"
				    )
			);
			foreach (var log in logsArray)
			{
				ConsoleLogger.Info("\u21AA " + log);
			}
			// if the dynamic object only has one property, it is the logs property, set response payload null
			//   (to prevent log: "ApiRequest::Perform for {fullServerUrl} got response when none was expected")
			if (payloadObj?.Count == 1)
			{
				responsePayload = null;
			}
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

		private static string HttpGet(
			string fullApiUrl,
			string? currentAccessToken = null,
			NameValueCollection? values = null
		) {
			if (values == null) values = new NameValueCollection();
			WebClient webclient = new WebClient();
			if (!string.IsNullOrEmpty(currentAccessToken))
			{
				webclient.Headers.Add(MSWConstants.APITokenHeader, "Bearer " + currentAccessToken);
			}
			webclient.Headers.Add("X-Server-Id", "019373cc-aa68-7d95-882f-9248ea338014");
			webclient.Headers.Add("X-Simulation-Name", Assembly.GetEntryAssembly()?.GetName().Name);
			byte[] response = webclient.UploadValues(fullApiUrl, values);
			return System.Text.Encoding.UTF8.GetString(response);
		}
	}
}
