using System.Linq;
using System.Reflection;

namespace TRViS.LocalServers.BveTs;

public partial class TimetableServerPlugin
{
	static readonly Assembly currentAssembly = Assembly.GetExecutingAssembly();

	public override string Location => currentAssembly.Location;

	public override string Name => currentAssembly.GetName().Name;

	public override string Title => "TRViS Timetable Server Plugin";

	public override string Version => currentAssembly.GetName().Version.ToString();

	public override string Description => $"({localAddressList.FirstOrDefault()}:{port}) Provides a timetable data server for TRViS";

	public override string Copyright => "Copyright 2023 Tetsu Otter";
}
