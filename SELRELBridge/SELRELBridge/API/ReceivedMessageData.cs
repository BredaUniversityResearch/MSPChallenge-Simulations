namespace SELRELBridge.API
{
	public class ReceivedMessageData
	{
		public readonly int m_messageIdentifier;
		public readonly string m_messagePayload;

		public ReceivedMessageData(int a_messageIdentifier, string a_messagePayload)
		{
			m_messageIdentifier = a_messageIdentifier;
			m_messagePayload = a_messagePayload;
		}
	}
}
