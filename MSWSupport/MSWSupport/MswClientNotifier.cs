using System;
using System.Net;
using System.Threading.Tasks;

namespace MSWSupport;

/// <summary>
/// Runs inside simulation processes. Subscribes to APIRequest.OnUnauthorizedAccess and
/// notifies MSW's ReportUnauthorized REST endpoint so MSW can immediately renew the token.
/// Call Initialize() once at simulation startup.
/// </summary>
public static class MswClientNotifier
{
	private static string? s_mswBaseEndpoint;
	private static string? s_apiEndpoint;
	private static DateTime s_lastUnauthorizedNotificationTime = DateTime.MinValue;
	private static DateTime s_lastSessionGoneNotificationTime = DateTime.MinValue;

	/// <summary>
	/// Minimum seconds between successive notifications to avoid flooding MSW.
	/// </summary>
	private const int NotificationCooldownSeconds = 15;

	/// <summary>
	/// Reads MSWEndpoint and APIEndpoint from the process command-line arguments.
	/// If MSWEndpoint is present, subscribes to APIRequest.OnUnauthorizedAccess.
	/// </summary>
	public static void Initialize()
	{
		string[] args = Environment.GetCommandLineArgs();
		foreach (string arg in args)
		{
			if (arg.StartsWith(MSWConstants.MSWEndpointCommandLineArgument + "=", StringComparison.OrdinalIgnoreCase))
			{
				s_mswBaseEndpoint = arg.Substring(MSWConstants.MSWEndpointCommandLineArgument.Length + 1);
			}
			else if (arg.StartsWith("APIEndpoint=", StringComparison.OrdinalIgnoreCase))
			{
				s_apiEndpoint = arg.Substring("APIEndpoint=".Length);
			}
		}

		if (!string.IsNullOrEmpty(s_mswBaseEndpoint))
		{
			APIRequest.OnUnauthorizedAccess += OnApiUnauthorizedAccess;
			APIRequest.OnSessionApiGone += OnSessionApiGone;
			ConsoleLogger.Info($"MSW simulation notifications enabled, reporting to: {s_mswBaseEndpoint}");
		}
	}

	private static void OnApiUnauthorizedAccess(string serverUrl)
	{
		// Apply cooldown to avoid spamming MSW with notifications during a 401 retry loop
		lock (typeof(MswClientNotifier))
		{
			if ((DateTime.Now - s_lastUnauthorizedNotificationTime).TotalSeconds < NotificationCooldownSeconds)
				return;
			s_lastUnauthorizedNotificationTime = DateTime.Now;
		}

		// Prefer the known API endpoint (from cmdline) so MSW can match it to a ServerData
		string reportedEndpoint = s_apiEndpoint ?? serverUrl;
		Task.Run(() => SendUnauthorizedNotification(reportedEndpoint));
	}

	private static void OnSessionApiGone(string serverUrl)
	{
		// Apply cooldown to avoid spamming MSW with notifications during a 410 retry loop
		lock (typeof(MswClientNotifier))
		{
			if ((DateTime.Now - s_lastSessionGoneNotificationTime).TotalSeconds < NotificationCooldownSeconds)
				return;
			s_lastSessionGoneNotificationTime = DateTime.Now;
		}

		// Send synchronously to maximize chance this reaches MSW before process exit.
		string reportedEndpoint = s_apiEndpoint ?? serverUrl;
		SendSessionGoneNotification(reportedEndpoint);
	}

	private static void SendUnauthorizedNotification(string apiEndpoint)
	{
		try
		{
			// Build the notification URL, avoiding APIRequest.Perform to prevent event re-entrancy
			string notificationUrl = s_mswBaseEndpoint.TrimEnd('/') + "/" + MSWConstants.MSWReportUnauthorizedEndpoint;
			string body = "game_session_api=" + Uri.EscapeDataString(apiEndpoint);

			using WebClient client = new WebClient();
			client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
			client.UploadString(notificationUrl, body);
			ConsoleLogger.Info($"Notified MSW of unauthorized access for {apiEndpoint}");
		}
		catch (Exception ex)
		{
			// Notification failure is non-fatal – MSW will still renew via its own health check
			ConsoleLogger.Warning($"Failed to notify MSW of unauthorized access: {ex.Message}");
		}
	}

	private static void SendSessionGoneNotification(string apiEndpoint)
	{
		try
		{
			string notificationUrl = s_mswBaseEndpoint.TrimEnd('/') + "/" + MSWConstants.MSWReportSessionGoneEndpoint;
			string body = "game_session_api=" + Uri.EscapeDataString(apiEndpoint);

			using WebClient client = new WebClient();
			client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
			client.UploadString(notificationUrl, body);
			ConsoleLogger.Info($"Notified MSW of session-gone state for {apiEndpoint}");
		}
		catch (Exception ex)
		{
			ConsoleLogger.Warning($"Failed to notify MSW of session-gone state: {ex.Message}");
		}
	}
}
