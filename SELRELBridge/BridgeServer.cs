using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SELRELBridge.API;

namespace SELRELBridge
{
	public class BridgeServer : IDisposable
	{
		private static readonly TimeSpan BroadcastMessageInterval = TimeSpan.FromSeconds(2.5f);

		private UdpClient m_broadcastSendClient = new UdpClient(0, AddressFamily.InterNetwork);
		private Thread m_broadcastThread;
		private TcpListener m_tcpListener = new TcpListener(IPAddress.Any, 0);
		private string m_watchdogToken = null;

		private BridgeConnection m_currentConnectedBridge = null;

		private SELOutputData m_cachedResult = null;

		public BridgeServer(string a_watchdogToken)
		{
			m_watchdogToken = a_watchdogToken;

			m_broadcastSendClient.Ttl = 0; //Ensure multicast does not leave the local machine.
			m_broadcastSendClient.JoinMulticastGroup(MSWBridgeConstants.DiscoveryMulticastAddress);
			m_broadcastThread = new Thread(DoBackgroundBroadcastWork);
			m_broadcastThread.Start();

			m_tcpListener.Start(1);
			m_tcpListener.BeginAcceptTcpClient(OnClientConnectionRequest, m_tcpListener);
		}

		private void OnClientConnectionRequest(IAsyncResult a_ar)
		{
			Console.WriteLine("SELRELBridge\t| Client connected");
			if (m_currentConnectedBridge != null)
			{
				Console.WriteLine("Client not properly disconnected before new connection appeared");
				m_currentConnectedBridge.CloseConnection();
			}

			m_currentConnectedBridge = new BridgeConnection(m_tcpListener.EndAcceptTcpClient(a_ar), OnClientDisconnected);

			if (m_cachedResult != null)
			{
				string json = JsonConvert.SerializeObject(m_cachedResult);
				m_currentConnectedBridge.SendData(SELOutputData.MessageIdentifier, json);
			}

			m_tcpListener.Stop();
		}

		private void OnClientDisconnected()
		{
			Console.WriteLine("SELRELBridge\t| Client disconnected");
			m_currentConnectedBridge = null;

			m_tcpListener.Start(1);
			m_tcpListener.BeginAcceptTcpClient(OnClientConnectionRequest, m_tcpListener);
		}

		private void DoBackgroundBroadcastWork()
		{
			while (true)
			{
				if (m_currentConnectedBridge == null)
				{
					DiscoveryBroadcastMessage message = new DiscoveryBroadcastMessage() { watchdog_token = m_watchdogToken, sel_connection_port = ((IPEndPoint)m_tcpListener.LocalEndpoint).Port };
					string json = JsonConvert.SerializeObject(message);
					byte[] dataGram = Encoding.UTF8.GetBytes(json);
					m_broadcastSendClient.Send(dataGram, dataGram.Length, new IPEndPoint(MSWBridgeConstants.DiscoveryMulticastAddress, MSWBridgeConstants.DiscoveryMulticastPort));
				}

				Thread.Sleep(BroadcastMessageInterval);
			}
		}

		public void Submit(SELOutputData a_data)
		{
			m_cachedResult = a_data;
			if (m_currentConnectedBridge != null)
			{
				string json = JsonConvert.SerializeObject(a_data);
				m_currentConnectedBridge.SendData(SELOutputData.MessageIdentifier, json);
			}
		}

		public void Dispose()
		{
			m_broadcastSendClient?.Dispose();
		}
	}
}
