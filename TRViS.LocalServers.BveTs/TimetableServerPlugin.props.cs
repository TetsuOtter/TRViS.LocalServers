using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace TRViS.LocalServers.BveTs;

public partial class TimetableServerPlugin
{
	static readonly Assembly currentAssembly = Assembly.GetExecutingAssembly();

	public override string Location => currentAssembly.Location;

	public override string Name => currentAssembly.GetName().Name;

	public override string Title => "TRViS Timetable Server Plugin";

	public override string Version => currentAssembly.GetName().Version.ToString();

	IPAddress? ipv4Address => localAddressList.FirstOrDefault(v => v.AddressFamily == AddressFamily.InterNetwork);
	public override string Description => $"({ipv4Address ?? localAddressList.FirstOrDefault()}:{port}) Provides a timetable data server for TRViS";

	public override string Copyright => "Copyright 2023 Tetsu Otter";
}
