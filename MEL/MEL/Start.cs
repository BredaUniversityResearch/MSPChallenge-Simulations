using System;
using System.IO;
using MSWSupport;
namespace MEL
{
    class Start
    {
        public static void Main(string[] args)
        {
	        ConsoleTextWriter.Instance.SetMessageFormat("{prefix}{message}");
	        ConsoleTextWriter.Instance.SetMessageParameter("prefix", "MEL: ");
			Console.SetOut(ConsoleTextWriter.Instance);
            MEL mel = new MEL();
            while(true) {
				System.Threading.Thread.Sleep(MEL.TICK_DELAY_MS);
				APIRequest.SleepOnApiUnauthorizedWebException(() => mel.Tick());
			}
		}
	}
}
