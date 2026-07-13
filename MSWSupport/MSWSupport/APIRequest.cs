using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
		private const int DEFAULT_TRANSIENT_HTTP_MAX_ATTEMPTS = 4;
		private const int DEFAULT_TRANSIENT_HTTP_BASE_DELAY_MS = 500;
		private const int DEFAULT_TRANSIENT_HTTP_MAX_DELAY_MS = 8000;

		/// <summary>
		/// Fired whenever an API call receives a 401 Unauthorized response.
		/// The string parameter is the server base URL that returned the 401.
		/// Subscribers (e.g. MswClientNotifier) use this to notify MSW for immediate token renewal.
		/// </summary>
		public static event Action<string>? OnUnauthorizedAccess;

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
				ConsoleLogger.Warning(string.Format(messageFormat, sleepSec), ex);
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
					ConsoleLogger.Info($"ApiRequest::Perform for {serverUrl}/{apiUrl} got response when none was expected. Response: {jsonResult}");
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
			string fullServerUrl = BuildFullApiUrl(serverUrl, apiUrl);
			string response = null;
			try
			{
				int attempt = 1;
				while (true)
				{
					try
					{
						response = HttpGet(fullServerUrl, currentAccessToken, postValues);
						break;
					}
					catch (WebException ex)
					{
						if (!ShouldRetryWebException(ex, attempt, out int delayMs, out string retryReason))
						{
							throw;
						}
						ConsoleLogger.Warning(
							$"ApiRequest::Perform transient failure for {fullServerUrl} ({retryReason}). Retrying in {delayMs} ms (attempt {attempt + 1}/{DEFAULT_TRANSIENT_HTTP_MAX_ATTEMPTS})",
							ex
						);
						Thread.Sleep(delayMs);
						attempt++;
					}
				}
			}
			catch (WebException ex)
			{
				HttpWebResponse? httpResponse = ex.Response as HttpWebResponse;
				if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.Unauthorized)
				{
					OnUnauthorizedAccess?.Invoke(serverUrl);
					throw new ApiUnauthorizedWebException(ex); // allow child code to handle this one
				}
				if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.Gone)
				{
					throw new SessionApiGoneWebException(ex); // allow child code to handle this one
				}

				string? responseBody = null;
				if (ex.Response != null)
				{
					try
					{
						using var stream = ex.Response.GetResponseStream();
						if (stream != null)
						{
							using var reader = new StreamReader(stream);
							responseBody = reader.ReadToEnd();
						}
					}
					catch (Exception responseReadException)
					{
						ConsoleLogger.Warning($"ApiRequest::Perform for {fullServerUrl} failed to read error response body", responseReadException);
					}
				}

				string? responseMessage = TryExtractMessageFromJsonResponse(responseBody);
				string statusCode = httpResponse != null ? ((int)httpResponse.StatusCode).ToString() : "unknown";
				string statusDescription = httpResponse?.StatusDescription ?? "n/a";
				string contentType = ex.Response?.ContentType ?? "unknown";

				var contextDict = new Dictionary<string, object>();
				contextDict.Add("exception", ConsoleLogger.SerializeException(ex));
				contextDict.Add("statusCode", statusCode);
				contextDict.Add("statusDescription", statusDescription);
				contextDict.Add("contentType", contentType);
				if (!string.IsNullOrEmpty(responseMessage))
				{
					contextDict.Add("message", responseMessage);
				}
				if (!string.IsNullOrEmpty(responseBody))
				{
					contextDict.Add("responseBodySnippet", ClipForLog(responseBody));
				}
				ConsoleLogger.Warning($"ApiRequest::Perform for {fullServerUrl} failed with HTTP error: {ex.Message}", contextDict);
				responsePayload = null;
				return false;
			}
			catch (Exception ex)
			{
				var contextDict = new Dictionary<string, object>();
				contextDict.Add("exception", ConsoleLogger.SerializeException(ex));
				ConsoleLogger.Warning($"ApiRequest::Perform for {fullServerUrl} failed with unexpected exception", contextDict);
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
						postValues == null || postValues.Count == 0 ? "" : " with post values\n" +
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

		private static string BuildFullApiUrl(string serverUrl, string apiUrl)
		{
			if (string.IsNullOrWhiteSpace(serverUrl))
			{
				return apiUrl ?? string.Empty;
			}
			if (string.IsNullOrWhiteSpace(apiUrl))
			{
				return serverUrl;
			}
			return serverUrl.TrimEnd('/') + "/" + apiUrl.TrimStart('/');
		}

		private static bool ShouldRetryWebException(WebException ex, int attempt, out int delayMs, out string retryReason)
		{
			delayMs = 0;
			retryReason = "";
			if (attempt >= DEFAULT_TRANSIENT_HTTP_MAX_ATTEMPTS)
			{
				return false;
			}

			if (ex.Response is HttpWebResponse httpResponse)
			{
				if (!IsTransientStatusCode(httpResponse.StatusCode))
				{
					return false;
				}
				retryReason = $"HTTP {(int)httpResponse.StatusCode} {httpResponse.StatusDescription}";
			}
			else
			{
				if (!IsTransientWebExceptionStatus(ex.Status))
				{
					return false;
				}
				retryReason = $"WebExceptionStatus.{ex.Status}";
			}

			delayMs = ComputeRetryDelayMs(attempt);
			return true;
		}

		private static bool IsTransientStatusCode(HttpStatusCode statusCode)
		{
			return statusCode switch
			{
				HttpStatusCode.RequestTimeout => true,
				HttpStatusCode.BadGateway => true,
				HttpStatusCode.ServiceUnavailable => true,
				HttpStatusCode.GatewayTimeout => true,
				(HttpStatusCode)429 => true,
				_ => false
			};
		}

		private static bool IsTransientWebExceptionStatus(WebExceptionStatus status)
		{
			return status == WebExceptionStatus.Timeout ||
			       status == WebExceptionStatus.ConnectFailure ||
			       status == WebExceptionStatus.NameResolutionFailure ||
			       status == WebExceptionStatus.ProxyNameResolutionFailure ||
			       status == WebExceptionStatus.ConnectionClosed ||
			       status == WebExceptionStatus.ReceiveFailure ||
			       status == WebExceptionStatus.SendFailure ||
			       status == WebExceptionStatus.KeepAliveFailure;
		}

		private static int ComputeRetryDelayMs(int attempt)
		{
			int exponent = Math.Max(0, attempt - 1);
			int backoffMs = DEFAULT_TRANSIENT_HTTP_BASE_DELAY_MS * (1 << Math.Min(exponent, 5));
			return Math.Min(backoffMs, DEFAULT_TRANSIENT_HTTP_MAX_DELAY_MS);
		}

		private static string? TryExtractMessageFromJsonResponse(string? responseBody)
		{
			if (string.IsNullOrWhiteSpace(responseBody))
			{
				return null;
			}

			string trimmed = responseBody.TrimStart();
			if (trimmed.StartsWith("<"))
			{
				ConsoleLogger.Warning(
					"ApiRequest received a non-JSON error response body (looks like HTML/XML).",
					new Dictionary<string, object>
					{
						{ "responseBodySnippet", ClipForLog(responseBody) }
					}
				);
				return null;
			}

			try
			{
				var json = JObject.Parse(responseBody);
				return json["message"]?.ToString();
			}
			catch (JsonReaderException ex)
			{
				ConsoleLogger.Warning(
					"ApiRequest received an error response body that is not valid JSON.",
					new Dictionary<string, object>
					{
						{ "exception", ConsoleLogger.SerializeException(ex) },
						{ "responseBodySnippet", ClipForLog(responseBody) }
					}
				);
				return null;
			}
		}

		private static string ClipForLog(string data, int maxLength = 400)
		{
			if (string.IsNullOrEmpty(data) || data.Length <= maxLength)
			{
				return data;
			}
			return data.Substring(0, maxLength) + "...";
		}

		private static TOutputType DeserializeJson<TOutputType>(string jsonData)
		{
			try
			{
				return JsonConvert.DeserializeObject<TOutputType>(jsonData);
			}
			catch (JsonReaderException ex)
			{
				var context = new
				{
					exception = ConsoleLogger.SerializeException(ex),
					inputData = jsonData
				};
				ConsoleLogger.Warning("Error deserializing JSON String.", context);
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
