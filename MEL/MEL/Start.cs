namespace MEL
{
    class Start
    {
        public static void Main(string[] args)
        {
            MEL mel = new MEL();
			while(true) {
				System.Threading.Thread.Sleep(MEL.TICK_DELAY_MS);
				mel.Tick();
			}
		}
	}
}
