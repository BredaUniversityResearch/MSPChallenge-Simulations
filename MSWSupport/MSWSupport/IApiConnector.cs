using System.Collections.Specialized;
namespace MSWSupport;

public interface IApiConnector : ITokenReceiver, IUpdateMonthReceiver
{
	public bool CheckApiAccess();
	public int ApiRequestCurrentMonth();
	public int GetCurrentGameMonth();
	public string GetServerUrl();
	public string GetAccessToken();
}
