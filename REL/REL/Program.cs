extern alias AsposeDrawing;
using System.Reflection;
using AsposeDrawing::Aspose.Drawing;

namespace REL
{
	static class Program
	{
		static void Main(string[] a_args)
		{
			try
			{
				LoadLicense();
				Console.WriteLine("Starting Samson Integration for MSP (REL)...");

				RiskModel model = new RiskModel();
				model.Run();
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
