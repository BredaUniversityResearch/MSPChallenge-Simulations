extern alias AsposeDrawing;
using System.Reflection;
using AsposeDrawing::Aspose.Drawing;

namespace SEL
{
	class Program
	{
		private const int TICKRATE = 500; //ms

		static void Main(string[] args)
		{
			try
			{
				LoadLicense();

				Console.WriteLine("Starting MSP2050 Shipping EmuLation version {0}", typeof(Program).Assembly.GetName().Version);

				ShippingModel model = new ShippingModel();
				while (true)
				{
					model.Tick();
					Thread.Sleep(TICKRATE);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message + "\n" + e.StackTrace);
			}
		}

		static void LoadLicense()
		{
			License license = new();
			const string licenseFilename = "Aspose.Drawing.NET.lic";
			if (File.Exists(licenseFilename)) // load from working dir
			{
				license.SetLicense(licenseFilename);
				return;
			}

			// load from shared development folder
			var appName = Assembly.GetExecutingAssembly().GetName().Name;
			DirectoryInfo? dir = new(Environment.CurrentDirectory);
			if (dir == null)
			{
				throw new Exception("Could not retrieve current dir");
			}
			while (dir.Name != appName) {
				dir = Directory.GetParent(dir.FullName);
			}
			var licensePath = Path.GetFullPath(Path.Combine(dir.ToString(), @"..\..\" + licenseFilename));
			license.SetLicense(licensePath);
		}
	}
}
