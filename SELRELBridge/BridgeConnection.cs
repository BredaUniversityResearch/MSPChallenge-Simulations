using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SELRELBridge.API;

namespace SELRELBridge
{
	internal class BridgeConnection
	{
		private const int BytesToMegaBytes = 1024 * 1024;

		public delegate void OnConnectionClosed();

		private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan KeepAliveFrequency = TimeSpan.FromSeconds(1);

		private TcpClient m_connection;
		private Thread m_keepAliveThread;
		private DateTime m_lastReceivedTime;
		private DateTime m_lastKeepAliveSendTime;

		private OnConnectionClosed m_onConnectionClosed;
		private List<ReceivedMessageData> m_receivedMessages = new List<ReceivedMessageData>(8);

		public BridgeConnection(TcpClient a_client, OnConnectionClosed a_onConnectionClosed)
		{
			m_connection = a_client;
			m_onConnectionClosed = a_onConnectionClosed;

			m_connection.ReceiveTimeout = 5000;
			m_keepAliveThread = new Thread(DoBackgroundClientWork);
			m_lastReceivedTime = DateTime.UtcNow;
			m_lastKeepAliveSendTime = DateTime.UtcNow;
			m_keepAliveThread.Start(this);
		}

		public void SendData(int a_messageIdentifier, string a_data)
		{
			if (m_connection != null && m_connection.Connected)
			{
				byte[] stringInBytes = Encoding.UTF8.GetBytes(a_data);

				lock (m_connection)
				{
					NetworkStream stream = m_connection.GetStream();

					try
					{
						stream.Write(BitConverter.GetBytes(a_messageIdentifier), 0, sizeof(int));
						stream.Write(BitConverter.GetBytes(stringInBytes.Length + 1), 0, sizeof(int));
						stream.Write(stringInBytes, 0, stringInBytes.Length);
						stream.WriteByte(0);
					}
					catch (IOException)
					{
						m_onConnectionClosed();
					}
				}
			}
		}

		public void ReadData(out int a_messageIdentifier, out string a_messageData)
		{
			a_messageIdentifier = -1;
			a_messageData = null;
			try
			{
				NetworkStream stream = m_connection.GetStream();

				byte[] buffer = new byte[4];
				int bytesRead = stream.Read(buffer, 0, sizeof(int));
				a_messageIdentifier = BitConverter.ToInt32(buffer, 0);
				if (bytesRead != sizeof(int))
				{
					Console.WriteLine(
						$"SELRELBridge\t| Malformed message received. Message type: {a_messageIdentifier}");
					return; //IDK what we received...
				}

				//Unlikely this should happen, but wait for read until very long.
				int originalTimeout = m_connection.ReceiveTimeout;
				m_connection.ReceiveTimeout = 15000;

				bytesRead = stream.Read(buffer, 0, sizeof(int));
				int messageSize = BitConverter.ToInt32(buffer, 0);
				if (bytesRead != sizeof(int) || messageSize < 0 || messageSize > 50 * BytesToMegaBytes)
				{
					Console.WriteLine(
						$"SELRELBridge\t| Malformed message received. Bytes read: {bytesRead} / {sizeof(int)} Decoded message size: {messageSize}");
					return;
				}

				byte[] payload = new byte[messageSize];
				if (messageSize > 0)
				{
					bytesRead = 0;
					while (bytesRead < messageSize)
					{
						int bytesToRead = Math.Min(payload.Length - bytesRead, 8192);
						int thisBytesRead = stream.Read(payload, bytesRead, bytesToRead);
						bytesRead += thisBytesRead;
					}

					if (bytesRead != messageSize)
					{
						Console.WriteLine(
							$"SELRELBridge\t| Incomplete message received. Expected to read: {messageSize} only got {bytesRead}");
					}

					a_messageData = Encoding.UTF8.GetString(payload);
				}

				m_connection.ReceiveTimeout = originalTimeout;
			}
			catch (InvalidOperationException)
			{
			}
			catch (IOException)
			{
				//Read timeout
			}
		}

		public ReceivedMessageData[] GetReceivedMessages()
		{
			lock (m_receivedMessages)
			{
				ReceivedMessageData[] data = m_receivedMessages.ToArray();
				m_receivedMessages.Clear();
				return data;
			}
		}

		private void DoBackgroundClientWork(object a_obj)
		{
			BridgeConnection con = (BridgeConnection)a_obj;
			while (con.m_connection.Connected)
			{
				if (DateTime.UtcNow - m_lastKeepAliveSendTime > KeepAliveFrequency)
				{
					con.SendData(KeepAliveMessage.MessageIdentifier, "");
					m_lastKeepAliveSendTime = DateTime.UtcNow;
				}

				if (con.m_connection.Connected)
				{
					con.ReadData(out var messageIdentifier, out var messagePayload);

					if (messageIdentifier > 0)
					{
						m_lastReceivedTime = DateTime.UtcNow;
						if (messageIdentifier != KeepAliveMessage.MessageIdentifier)
						{
							lock (m_receivedMessages)
							{
								m_receivedMessages.Add(new ReceivedMessageData(messageIdentifier, messagePayload));
							}
						}
					}
				}

				if (DateTime.UtcNow - m_lastReceivedTime > ConnectionTimeout)
				{
					con.m_connection.Close();
				}
			}
			m_onConnectionClosed();
		}

		public void CloseConnection()
		{
			if (m_connection.Connected)
			{
				m_connection.Close();
				m_onConnectionClosed();
			}
		}
	}
}