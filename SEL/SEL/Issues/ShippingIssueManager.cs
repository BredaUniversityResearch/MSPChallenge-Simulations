using System.Collections.Generic;
using System.Text;
using SEL.API;

namespace SEL.Issues
{
	class ShippingIssueManager
	{
		private List<APIShippingIssue> m_currentIssues = new List<APIShippingIssue>(128);

		public void AddRoutingFailureIssue(ShippingPort source, ShippingPort destination, ShipType shipType)
		{
			StringBuilder issueText = new StringBuilder(128);
			issueText.Append("Failed to find route from \"");
			issueText.Append(source.PortName);
			issueText.Append("\" to \"");
			issueText.Append(destination.PortName);
			issueText.Append("\" for ship type \"");
			issueText.Append(shipType.ShipTypeName);
			issueText.Append("\"");
			CreateNewIssue(source, destination, issueText.ToString());
		}

		private void CreateNewIssue(ShippingPort source, ShippingPort destination, string message)
		{
			m_currentIssues.Add(new APIShippingIssue(source.GeometryPersistentId, destination.GeometryPersistentId, message));
		}

		public void ClearIssues()
		{
			m_currentIssues.Clear();
		}

		public void SubmitPendingIssues(IApiConnector apiConnector)
		{
			apiConnector.BatchPostIssues(m_currentIssues);
		}
	}
}
