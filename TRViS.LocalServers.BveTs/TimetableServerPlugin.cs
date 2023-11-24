using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AtsEx.PluginHost.Plugins;
using AtsEx.PluginHost.Plugins.Extensions;

using BveTypes.ClassWrappers;

using TRViS.JsonModels;

namespace TRViS.LocalServers.BveTs;

[PluginType(PluginType.Extension)]
public partial class TimetableServerPlugin : PluginBase, IExtension
{
	const string LISTENER_PATH = "/";
	const string TIMETABLE_FILE_MIME = "application/json";
	const string TIMETABLE_FILE_NAME = "timetable.json";
	const string QR_HTML_FILE_NAME = "qr.html";
	const string QRCODE_MIN_JS_FILE_NAME = "qrcode.min.js";
	const int LISTENER_PORT = 58600;
	const int PORT_RETRY_MAX = 10;
	readonly IPAddress[] localAddressList;
	int port = LISTENER_PORT;
	TcpListener listener = new(IPAddress.Any, LISTENER_PORT);
	private readonly CancellationTokenSource cts = new();

	public TimetableServerPlugin(PluginBuilder builder) : base(builder)
	{
		localAddressList = Dns.GetHostAddresses(Dns.GetHostName());
		StartListener();
		Task.Run(ListenTaskAsync);

		AddLaunchBrowserButtonToContextMenu();
	}

	void StartListener()
	{
		SocketException? _ex = null;

		for (int i = 0; i < PORT_RETRY_MAX; i++)
		{
			try
			{
				listener.Start();
				return;
			}
			catch (SocketException ex)
			{
				_ex = ex;
				if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
				{
					port = LISTENER_PORT + i + 1;
					listener = new TcpListener(IPAddress.Any, port);
					continue;
				}
				else
					throw;
			}
		}

		throw new InvalidOperationException("TimetableServerPlugin: Address already in use.", _ex);
	}

	private async Task ListenTaskAsync()
	{
		while (!cts.Token.IsCancellationRequested)
		{
			TcpClient? client = null;
			try
			{
				try
				{
					client = await listener.AcceptTcpClientAsync();
				}
				catch (ObjectDisposedException)
				{
					break;
				}

				if (cts.Token.IsCancellationRequested)
					break;

				if (!client.Connected)
					continue;

				using NetworkStream stream = client.GetStream();
				using StreamReader reader = new(stream);

				(byte[] header, byte[] response) = await AcceptTcpClientAsync(reader);
				await stream.WriteAsync(header, 0, header.Length);
				await stream.WriteAsync(response, 0, response.Length);
				await stream.FlushAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			finally
			{
				client?.Dispose();
			}
		}
	}

	async Task<(byte[], byte[])> AcceptTcpClientAsync(StreamReader reader)
	{
		string method;
		string path;
		string version;
		NameValueCollection headers = [];
		string requestLine = await reader.ReadLineAsync();
		string[] requestLineParts = requestLine.Split(' ');
		if (requestLineParts.Length != 3)
		{
			return GenResponse("400 Bad Request", "text/plain", "Bad Request (Invalid Request Line)");
		}

		method = requestLineParts[0];
		path = requestLineParts[1];
		version = requestLineParts[2];

		string headerLine;
		while (!string.IsNullOrEmpty(headerLine = await reader.ReadLineAsync()))
		{
			string[] headerLineParts = headerLine.Split([':'], 2);
			headers.Add(headerLineParts[0], headerLineParts[1].Trim());
		}

		bool isHead = method == "HEAD";
		if (method != "GET" && method != "HEAD")
		{
			return GenResponse("405 Method Not Allowed", "text/plain", "Method Not Allowed", isHead);
		}

		if (path == (LISTENER_PATH + QR_HTML_FILE_NAME) || path.StartsWith($"{LISTENER_PATH}{QR_HTML_FILE_NAME}?"))
			return GenResponseFromEmbeddedResource(QR_HTML_FILE_NAME, "text/html", isHead);
		else if (path == (LISTENER_PATH + QRCODE_MIN_JS_FILE_NAME) || path.StartsWith($"{LISTENER_PATH}{QRCODE_MIN_JS_FILE_NAME}?"))
			return GenResponseFromEmbeddedResource(QRCODE_MIN_JS_FILE_NAME, "text/javascript", isHead);

		if (path != (LISTENER_PATH + TIMETABLE_FILE_NAME))
		{
			return GenResponse("404 Not Found", "text/plain", "Not Found", isHead);
		}

		if (!BveHacker.IsScenarioCreated)
		{
			return GenResponse("204 No Content", "text/plain", "No Content (Scenario Not Loaded)", isHead);
		}

		byte[] content;
		try
		{
			content = GenerateJson();
		}
		catch (Exception ex)
		{
			return GenResponse("500 Internal Server Error", "text/plain", $"Internal Server Error: {ex.Message}\r\n{ex.StackTrace}", isHead);
		}

		return GenResponse("200 OK", TIMETABLE_FILE_MIME, GenerateJson(), isHead);
	}

	byte[] GenerateJson()
	{
		Scenario scenario = BveHacker.Scenario;
		ScenarioInfo scenarioInfo = BveHacker.ScenarioInfo;
		StationList stations = scenario.Route.Stations;
		TimeTable timeTable = scenario.TimeTable;

		TimetableRowData[] trvisTimetableRows = new TimetableRowData[stations.Count];
		for (int i = 0; i < trvisTimetableRows.Length; i++)
		{
			int indexOfTimetableInstance = i + 1;
			bool IsLastStop = i == (trvisTimetableRows.Length - 1);

			trvisTimetableRows[i] = new TimetableRowData(
				StationName: timeTable.NameTexts[indexOfTimetableInstance],
				Location_m: stations[i].Location,
				Longitude_deg: null,
				Latitude_deg: null,
				OnStationDetectRadius_m: null,
				FullName: timeTable.NameTexts[indexOfTimetableInstance],
				RecordType: null,
				TrackName: null,
				DriveTime_MM: null,
				DriveTime_SS: null,
				IsOperationOnlyStop: false,
				IsPass: false,
				HasBracket: false,
				IsLastStop: IsLastStop,
				Arrive: timeTable.ArrivalTimeTexts[indexOfTimetableInstance],
				Departure: timeTable.DepertureTimeTexts[indexOfTimetableInstance],
				RunInLimit: null,
				RunOutLimit: null,
				Remarks: null,
				MarkerColor: null,
				MarkerText: null,
				WorkType: null
			);
		}

		double motorCarCount = scenario.Vehicle.Dynamics.MotorCar.Count;
		double trailerCarCount = scenario.Vehicle.Dynamics.TrailerCar.Count;
		string motorCarCountStr = 0 < motorCarCount ? $"{motorCarCount:#.#}M" : string.Empty;
		string trailerCarCountStr = 0 < trailerCarCount ? $"{trailerCarCount:#.#}T" : string.Empty;
		TrainData trvisTrainData = new(
			TrainNumber: scenarioInfo.Title,
			MaxSpeed: null,
			SpeedType: null,
			NominalTractiveCapacity: $"{scenarioInfo.VehicleTitle}\n{motorCarCountStr}{trailerCarCountStr}",
			CarCount: (int)Math.Ceiling(motorCarCount + trailerCarCount),
			Destination: null,
			BeginRemarks: null,
			AfterRemarks: null,
			Remarks: "Generated with TRViS Local Servers (AtsEX Extension)",
			BeforeDeparture: null,
			TrainInfo: null,
			Direction: 1,
			WorkType: null,
			AfterArrive: null,
			BeforeDeparture_OnStationTrackCol: null,
			AfterArrive_OnStationTrackCol: null,
			DayCount: 0,
			IsRideOnMoving: false,
			Color: null,
			TimetableRows: trvisTimetableRows
		);

		WorkData trvisWorkData = new(
			Name: scenarioInfo.RouteTitle,
			AffectDate: null,
			AffixContentType: null,
			AffixContent: null,
			Remarks: null,
			HasETrainTimetable: false,
			ETrainTimetableContentType: null,
			ETrainTimetableContent: null,
			Trains: [trvisTrainData]
		);

		WorkGroupData trvisWorkGroupData = new(
			Name: "TRViS Local Servers (AtsEX Extension)",
			DBVersion: 1,
			Works: [trvisWorkData]
		);

		return JsonSerializer.SerializeToUtf8Bytes<WorkGroupData[]>([trvisWorkGroupData], new JsonSerializerOptions { WriteIndented = false });
	}

	private static (byte[], byte[]) GenResponseFromEmbeddedResource(string fileName, string contentType, bool isHead = false)
	{
		using Stream stream = currentAssembly.GetManifestResourceStream($"TRViS.LocalServers.BveTs.{fileName}");
		long length = stream.Length;
		byte[] content = new byte[length];
		stream.Read(content, 0, (int)length);
		return GenResponse("200 OK", contentType, content, isHead);
	}

	private static (byte[], byte[]) GenResponse(string status, string contentType, string content, bool isHead = false)
		=> GenResponse(status, contentType, Encoding.UTF8.GetBytes(content), isHead);

	private static (byte[], byte[]) GenResponse(string status, string contentType, byte[] content, bool isHead = false)
	{
		StringBuilder sb = new();

		sb.AppendLine($"HTTP/1.0 {status}");
		sb.AppendLine($"Server: TRViS Local Servers (AtsEX Extension)");
		sb.AppendLine($"Content-Type: {contentType}; charset=UTF-8");
		sb.AppendLine($"Content-Length: {content.Length}");
		sb.AppendLine($"Date: {DateTime.UtcNow:R}");
		sb.AppendLine($"Connection: close");
		sb.AppendLine();
		if (isHead)
			content = [];

		string response = sb.ToString();
		return (Encoding.UTF8.GetBytes(response), content);
	}

	public override TickResult Tick(TimeSpan elapsed) => new ExtensionTickResult();

	public override void Dispose()
	{
		cts.Cancel();
		cts.Dispose();
		listener.Stop();
	}
}
