using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SELRELBridge.API;

namespace SELRELBridge
{
	public class BridgeClient: IDisposable
	{
		public delegate void MessageReceivedCallback(int a_messageType, string a_messageData);

		private UdpClient m_broadcastUdpClient = new UdpClient();
		private string m_watchdogToken;
		private BridgeConnection m_currentConnection = null;

		public BridgeClient(string a_watchdogToken)
		{
			m_watchdogToken = a_watchdogToken;

			m_broadcastUdpClient.ExclusiveAddressUse = false;
			m_broadcastUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			m_broadcastUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, MSWBridgeConstants.DiscoveryMulticastPort));
			m_broadcastUdpClient.JoinMulticastGroup(MSWBridgeConstants.DiscoveryMulticastAddress);
			m_broadcastUdpClient.BeginReceive(OnBroadcastReceived, m_broadcastUdpClient);
		}

		public void Dispose()
		{
			m_broadcastUdpClient.DropMulticastGroup(MSWBridgeConstants.DiscoveryMulticastAddress);
			m_broadcastUdpClient?.Dispose();
		}

		public void PumpReceivedMessages(MessageReceivedCallback a_callback)
		{
			if (m_currentConnection != null)
			{
				ReceivedMessageData[] receivedMessages = m_currentConnection.GetReceivedMessages();
				foreach (ReceivedMessageData data in receivedMessages)
				{
					a_callback(data.m_messageIdentifier, data.m_messagePayload);
				}
			}
		}

		private void OnBroadcastReceived(IAsyncResult a_ar)
		{
			try
			{
				UdpClient client = (UdpClient) a_ar.AsyncState;
				IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] receivedBytes = m_broadcastUdpClient.EndReceive(a_ar, ref receivedIpEndPoint);

				string result = Encoding.UTF8.GetString(receivedBytes);
				DiscoveryBroadcastMessage broadcast = JsonConvert.DeserializeObject<DiscoveryBroadcastMessage>(result);

				if (broadcast.watchdog_token == m_watchdogToken && m_currentConnection == null)
				{
					TryConnectToBridgeServer(new IPEndPoint(receivedIpEndPoint.Address, broadcast.sel_connection_port));
				}

				client.BeginReceive(OnBroadcastReceived, a_ar.AsyncState);
			}
			catch (ObjectDisposedException)
			{
				//Swallow exception, normal shutdown procedure...
			}
		}

		private void TryConnectToBridgeServer(IPEndPoint a_targetEndpoint)
		{
			TcpClient client = new TcpClient();
			client.BeginConnect(a_targetEndpoint.Address, a_targetEndpoint.Port, OnConnectedToBridgeServer, client);
		}

		private void OnConnectedToBridgeServer(IAsyncResult a_ar)
		{
			if (m_currentConnection != null)
			{
				Console.WriteLine("SELRELBridge\t| Connected to server, but there's already a bridge connection active!");
				return;
			}

			TcpClient client = (TcpClient) a_ar.AsyncState;
			Console.WriteLine("SELRELBridge\t| Success Connecting to server");
			client.EndConnect(a_ar);
			m_currentConnection = new BridgeConnection(client, OnBridgeDisconnected);
		}

		private void OnBridgeDisconnected()
		{
			Console.WriteLine("SELRELBridge\t| Disconnected from server");
			m_currentConnection = null;
		}
	}
}
