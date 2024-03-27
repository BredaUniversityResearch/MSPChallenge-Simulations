using System;
using System.Collections.Specialized;
using Newtonsoft.Json;
namespace MSWSupport;

public class ApiConnectorBase: IApiConnector
{
	private string m_accessToken = "";
	private readonly string m_serverUrl;
	// ReSharper disable once ClassNeverInstantiated.Local
	private class ApiMonthContainer
	{
		// ReSharper disable once InconsistentNaming
		public int game_currentmonth = 0;
	}
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
		if (!HttpGet("/api/Game/GetCurrentMonth", null, out ApiMonthContainer result))
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
		if (HttpGet("/api/game/IsOnline", out string result))
		{
			return result == "online";
		}

		return false;
	}

	public bool HttpSet(string apiUrl, NameValueCollection? postValues = null)
	{
		postValues ??= new NameValueCollection();
		return APIRequest.Perform(m_serverUrl, apiUrl, m_accessToken, postValues);
	}

	public bool HttpSet<TTargetType>(string apiUrl, NameValueCollection? postValues, out TTargetType result, JsonSerializer? jsonSerializer = null)
	{
		postValues ??= new NameValueCollection();
		return null != jsonSerializer ? APIRequest.Perform(m_serverUrl, apiUrl, m_accessToken, postValues, out result, jsonSerializer) :
			APIRequest.Perform(m_serverUrl, apiUrl, m_accessToken, postValues, out result);
	}

	public bool HttpGet<TTargetType>(string apiUrl, out TTargetType result)
	{
		return HttpGet(apiUrl, null, out result);
	}

	public bool HttpGet<TTargetType>(string apiUrl, NameValueCollection? postValues, out TTargetType result)
	{
		postValues ??= new NameValueCollection();
		return APIRequest.Perform(m_serverUrl, apiUrl, m_accessToken, postValues, out result);
	}
}
