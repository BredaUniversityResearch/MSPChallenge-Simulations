using System;
using System.Collections.Specialized;
using Newtonsoft.Json;
namespace MSWSupport;

abstract public class ApiConnectorBase: IApiConnector
{
	private string m_accessToken = "";
	private readonly string m_serverUrl;
	// ReSharper disable once ClassNeverInstantiated.Local
	private class ApiMonthContainer
	{
		// ReSharper disable once InconsistentNaming
		public int game_currentmonth = 0;
	}
	// ReSharper disable once ClassNeverInstantiated.Local
	private class WatchdogTokenResponse
	{
		// ReSharper disable once InconsistentNaming
		public string watchdog_token = null;
	};	
	private int m_currentMonth = -100;
	private DateTime m_currentMonthUpdateTime = DateTime.MinValue;

	public ApiConnectorBase(string serverUrl)
	{
		m_serverUrl = serverUrl;
	}

	public string GetServerUrl()
	{
		return m_serverUrl;
	}

	public string GetAccessToken()
	{
		return m_accessToken;
	}

	public void UpdateAccessToken(string newAccessToken)
	{
		m_accessToken = newAccessToken;
	}

	public int ApiRequestCurrentMonth()
	{
		if (!HttpGet("/api/Game/GetCurrentMonth", out ApiMonthContainer result))
			return m_currentMonth;
		m_currentMonth = result.game_currentmonth;
		m_currentMonthUpdateTime = DateTime.Now;
		return m_currentMonth;
	}

	public int GetCurrentGameMonth()
	{
		if (m_currentMonthUpdateTime.AddSeconds(30) < DateTime.Now) // fail-safe , sync current month every 30 sec.
		{
			var currentMonth = m_currentMonth;
			m_currentMonth = ApiRequestCurrentMonth();
			if (currentMonth != m_currentMonth)
			{
				Console.WriteLine("----> Requested and detected a month change to " + m_currentMonth + " <----");
			}
		}
		return m_currentMonth;
	}

	public void UpdateMonth(int monthId)
	{
		m_currentMonth = monthId;
		m_currentMonthUpdateTime = DateTime.Now;
	}

	public bool CheckApiAccess()
	{
		if (string.IsNullOrEmpty(m_accessToken))
		{
			throw new ApiUnauthorizedWebException(null);
		}			
		if (HttpGet("/api/game/IsOnline", out string result))
		{
			return result == "online";
		}
		return false;
	}

	public bool HttpSet(string apiUrl, NameValueCollection? postValues = null, bool logServerResponseLogs = false)
	{
		return APIRequest.Perform(m_serverUrl, apiUrl, m_accessToken, postValues, logServerResponseLogs);
	}

	public bool HttpSet<TTargetType>(
		string apiUrl,
		out TTargetType result,
		NameValueCollection? postValues = null,
		JsonSerializer? jsonSerializer = null,
		bool logServerResponseLogs = false
	) {
		return APIRequest.Perform(
			m_serverUrl,
			apiUrl,
			result: out result,
			m_accessToken,
			postValues,
			jsonSerializer,
			logServerResponseLogs
		);
	}

	public bool HttpGet<TTargetType>(
		string apiUrl,
		out TTargetType result,
		NameValueCollection? postValues = null,
		bool logServerResponseLogs = false
	) {
		postValues ??= new NameValueCollection();
		return APIRequest.Perform(
			m_serverUrl,
			apiUrl,
			out result,
			m_accessToken,
			postValues,
			logServerResponseLogs: logServerResponseLogs
		);
	}
	
	public string GetWatchdogTokenForServer()
	{
		try
		{
			if (HttpGet("/api/simulation/GetWatchdogTokenForServer", out WatchdogTokenResponse response)) {
				return response.watchdog_token;
			}
		}
		catch (ApiUnauthorizedWebException ex)
		{
			return string.Empty;
		}
		return string.Empty;
	}	
}
