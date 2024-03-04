using System;
using System.IO;
namespace MEL
{
    class Start
    {
        public static void Main(string[] args)
        {
	        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            MEL mel = new MEL();
            while(true) {
				System.Threading.Thread.Sleep(MEL.TICK_DELAY_MS);
				mel.Tick();
			}
		}
        
        static void CurrentDomain_UnhandledException(object aSender, UnhandledExceptionEventArgs aException)
        {
	        ConsoleLogger.Error(((Exception) aException.ExceptionObject).Message);
        }
	}
}
