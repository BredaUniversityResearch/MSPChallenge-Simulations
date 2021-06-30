using System;
using SEL.API;

namespace SEL
{
	class ErrorReporter
	{
		private static ErrorReporter Instance = null;

		private readonly IApiConnector m_Api = null;

		public ErrorReporter(IApiConnector apiConnector)
		{
			m_Api = apiConnector;
			if (Instance != null)
			{
				throw new Exception("Double initialization of the error reporter");
			}

			Instance = this;
		}

		public static void ReportError(EErrorSeverity severity, string message)
		{
			ReportErrorLocal(severity, message);
			Instance?.ReportErrorToServer(severity, message);
		}

		private static void ReportErrorLocal(EErrorSeverity severity, string message)
		{
			ConsoleColor colorToRestore = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ForegroundColor = colorToRestore;
		}

		private void ReportErrorToServer(EErrorSeverity severity, string message)
		{
			System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
			m_Api.ReportErrorMessage(severity, message, stackTrace.ToString());
		}
	}
}
