using System;

using TrainCrew;

using TRViS.LocalServers.Core;

namespace TRViS.LocalServers.TrainCrew;

class Program : IDisposable
{
	readonly TimetableServerTrainCrewBridge bridge;
	readonly TimetableServerCore server;
	static void Main(string[] _)
	{
		try
		{
			using Program program = new();
			while (true)
			{
				Console.WriteLine(typeof(Program).Assembly.FullName);
				Console.WriteLine(TimetableServerCore.currentAssembly.FullName);
				Console.WriteLine("Open this link to show QR code: {0}", program.BrowserLink);
				Console.WriteLine("Type 'exit' to close this server.");
				string line = Console.ReadLine();
				if (line == "exit")
					break;
				Console.WriteLine(".......");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Unexpected Error: {0}", ex);
		}
	}

	public Program()
	{
		TrainCrewInput.Init();
		bridge = new();
		server = new(bridge);
	}

	public void Dispose()
	{
		server.Dispose();
		TrainCrewInput.Dispose();
	}

	public string BrowserLink => server.BrowserLink;
}
