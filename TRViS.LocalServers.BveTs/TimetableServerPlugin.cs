using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using AtsEx.PluginHost.Plugins;
using AtsEx.PluginHost.Plugins.Extensions;

using BveTypes.ClassWrappers;

using TR.SimpleHttpServer;

using TRViS.JsonModels;

namespace TRViS.LocalServers.BveTs;

[PluginType(PluginType.Extension)]
public partial class TimetableServerPlugin : PluginBase, IExtension
{
	const string LISTENER_PATH = "/";
	const string JSON_FILE_MIME = "application/json";
	const string TIMETABLE_FILE_MIME = JSON_FILE_MIME;
	const string TIMETABLE_FILE_NAME = "timetable.json";
	const string QR_HTML_FILE_NAME = "index.html";
	const string SCENARIO_INFO_FILE_NAME = "scenario-info.json";
	const int LISTENER_PORT = 58600;
	const int PORT_RETRY_MAX = 10;
	readonly IPAddress[] localAddressList;
	readonly HttpServer server;
	readonly int port;

	readonly NameValueCollection additionalHeaders = [];

	public TimetableServerPlugin(PluginBuilder builder) : base(builder)
	{
		additionalHeaders.Add("Access-Control-Allow-Origin", "*");
		localAddressList = Dns.GetHostAddresses(Dns.GetHostName());
		server = StartListener(out port);

		AddLaunchBrowserButtonToContextMenu();
	}

	HttpServer StartListener(out int port)
	{
		SocketException? _ex = null;

		port = LISTENER_PORT;
		for (int i = 0; i < PORT_RETRY_MAX; i++)
		{
			HttpServer? server = null;
			try
			{
				server = new((ushort)port, httpHandler);
				server.Start();
				return server;
			}
			catch (SocketException ex)
			{
				server?.Dispose();
				_ex = ex;
				if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
				{
					port = LISTENER_PORT + i + 1;
					continue;
				}
				else
					throw;
			}
		}

		throw new InvalidOperationException("TimetableServerPlugin: Address already in use.", _ex);
	}

	private Task<HttpResponse> httpHandler(HttpRequest request)
	{
		try
		{
			return AcceptTcpClientAsync(request);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return Task.FromResult(new HttpResponse(
				status: "500 Internal Server Error",
				ContentType: "text/plain",
				additionalHeaders: additionalHeaders,
				body: $"Internal Server Error: {ex.Message}\r\n{ex.StackTrace}"
			));
		}
	}

	static bool IsPathAcceptable(string path)
		=> path switch
		{
			LISTENER_PATH or (LISTENER_PATH + QR_HTML_FILE_NAME) => true,
			LISTENER_PATH + TIMETABLE_FILE_NAME => true,
			LISTENER_PATH + SCENARIO_INFO_FILE_NAME => true,
			_ => false
		};

	async Task<HttpResponse> AcceptTcpClientAsync(HttpRequest request)
	{
		string path = request.Path;
		string pathWithoutQueryOrHash = path.Split('?', '#')[0];

		if (!IsPathAcceptable(pathWithoutQueryOrHash))
		{
			return new HttpResponse(
				status: "404 Not Found",
				ContentType: "text/plain",
				additionalHeaders: additionalHeaders,
				body: "Not Found"
			);
		}

		if (pathWithoutQueryOrHash is LISTENER_PATH or (LISTENER_PATH + QR_HTML_FILE_NAME))
			return await GenResponseFromEmbeddedResourceAsync(QR_HTML_FILE_NAME, "text/html", additionalHeaders);

		if (!BveHacker.IsScenarioCreated)
		{
			return new(
				status: "204 No Content",
				ContentType: "text/plain",
				additionalHeaders: additionalHeaders,
				body: "No Content (Scenario Not Loaded)"
			);
		}

		try
		{
			byte[] content = pathWithoutQueryOrHash switch
			{
				LISTENER_PATH + TIMETABLE_FILE_NAME => GenerateJson(),
				LISTENER_PATH + SCENARIO_INFO_FILE_NAME => GenerateScenarioInfoJson(),
				_ => throw new InvalidOperationException("TimetableServerPlugin: Invalid path.")
			};

			return new HttpResponse(
				status: "200 OK",
				ContentType: TIMETABLE_FILE_MIME,
				additionalHeaders: additionalHeaders,
				body: content
			);
		}
		catch (Exception ex)
		{
			return new HttpResponse(
				status: "500 Internal Server Error",
				ContentType: "text/plain",
				additionalHeaders: additionalHeaders,
				body: $"Internal Server Error: {ex.Message}\r\n{ex.StackTrace}"
			);
		}
	}

	static readonly JsonSerializerOptions GenerateScenarioInfo_JsonSerializerOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};
	private byte[] GenerateScenarioInfoJson()
	{
		ResponseTypes.ScenarioInfo scenarioInfo = new(
			routeName: BveHacker.ScenarioInfo.RouteTitle,
			scenarioName: BveHacker.ScenarioInfo.Title,
			carName: BveHacker.ScenarioInfo.VehicleTitle
		);

		return JsonSerializer.SerializeToUtf8Bytes(
			scenarioInfo,
			GenerateScenarioInfo_JsonSerializerOptions
		);
	}

	static string RemoveSpaceCharBetweenEachChar(string str)
	{
		str = str.Trim();
		if (str.Length < 2 || !char.IsWhiteSpace(str, 1))
			return str;

		int spaceCount = 0;
		for (int i = 1; i < str.Length; i++)
		{
			if (char.IsWhiteSpace(str, i))
				spaceCount++;
			else
				break;
		}

		if ((str.Length - 1) % (spaceCount + 1) != 0)
			return str;

		StringBuilder sb = new(((str.Length - 1) / (spaceCount + 1)) + 1);
		sb.Append(str[0]);
		for (int i = 1, iSpace = 0; i < str.Length; i++)
		{
			if (iSpace != spaceCount)
			{
				if (char.IsWhiteSpace(str, i))
					iSpace++;
				else
					return str;
			}
			else
			{
				if (char.IsWhiteSpace(str, i))
					return str;
				else
					iSpace = 0;
				sb.Append(str[i]);
			}
		}
		return sb.ToString();
	}
	static readonly JsonSerializerOptions GenerateJson_JsonSerializerOptions = new()
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};
	private static readonly Regex beginWithSpeedLimitRegex = new(@"^([\d-]+)\s*\/(.+)$", RegexOptions.Compiled);
	private static readonly Regex endWithSpeedLimitRegex = new(@"^(.+)\/\s*([\d-]+)$", RegexOptions.Compiled);
	byte[] GenerateJson()
	{
		Scenario scenario = BveHacker.Scenario;
		ScenarioInfo scenarioInfo = BveHacker.ScenarioInfo;
		TimeTable timeTable = scenario.TimeTable;

		Station[] stationArray = scenario.Route.Stations.Cast<Station>().ToArray();
		TimetableRowData[] trvisTimetableRows = new TimetableRowData[stationArray.Length];
		// 最後の駅以外ドアが開かない場合、その列車は非営業列車である -> ドアが開かない駅でも運転停車ではない
		bool isAllStationDoorNotOpenExceptLastStation = stationArray
			.Take(stationArray.Length - 1)
			.All(station => station.Pass || station.DoorSide == 0);
		for (int i = 0; i < trvisTimetableRows.Length; i++)
		{
			int indexOfTimetableInstance = i + 1;

			Station? station = stationArray[i];
			string staName = station.Name;
			string? remarks = null;
			if (beginWithSpeedLimitRegex.Match(staName) is Match beginMatch && beginMatch.Success)
			{
				remarks = beginMatch.Groups[1].Value;
				staName = beginMatch.Groups[2].Value;
			}
			else if (endWithSpeedLimitRegex.Match(staName) is Match endMatch && endMatch.Success)
			{
				staName = endMatch.Groups[1].Value;
				remarks = endMatch.Groups[2].Value;
			}
			staName = RemoveSpaceCharBetweenEachChar(station.Name);
			if (remarks is not null)
				remarks = $"駅間制限 {remarks}";
			bool isLastStop = station.IsTerminal;
			bool isPass = station.Pass;
			string arriveStr = timeTable.ArrivalTimeTexts[indexOfTimetableInstance];
			string departureStr = timeTable.DepertureTimeTexts[indexOfTimetableInstance];
			trvisTimetableRows[i] = new TimetableRowData(
				StationName: staName,
				Location_m: station.Location,
				Longitude_deg: null,
				Latitude_deg: null,
				OnStationDetectRadius_m: null,
				FullName: timeTable.NameTexts[indexOfTimetableInstance],
				RecordType: null,
				TrackName: null,
				DriveTime_MM: null,
				DriveTime_SS: null,
				IsOperationOnlyStop: !isPass && !isAllStationDoorNotOpenExceptLastStation && station.DoorSide == 0,
				IsPass: isPass,
				HasBracket: i == 0 && !string.IsNullOrEmpty(arriveStr),
				IsLastStop: isLastStop,
				Arrive: arriveStr,
				Departure: isLastStop ? null : departureStr,
				RunInLimit: null,
				RunOutLimit: null,
				Remarks: remarks,
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
			Remarks: "Generated with TRViS Local Servers (AtsEX Extension)\n" + scenarioInfo.Comment,
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

		return JsonSerializer.SerializeToUtf8Bytes<WorkGroupData[]>([trvisWorkGroupData], GenerateJson_JsonSerializerOptions);
	}

	private static async Task<HttpResponse> GenResponseFromEmbeddedResourceAsync(string fileName, string contentType, NameValueCollection additionalHeaders)
	{
		using Stream stream = currentAssembly.GetManifestResourceStream($"TRViS.LocalServers.BveTs.{fileName}");
		long length = stream.Length;
		byte[] content = new byte[length];
		await stream.ReadAsync(content, 0, (int)length);

		return new HttpResponse(
			status: "200 OK",
			ContentType: contentType,
			additionalHeaders: additionalHeaders,
			body: content
		);
	}

	public override TickResult Tick(TimeSpan elapsed) => new ExtensionTickResult();

	public override void Dispose()
	{
		server.Stop();
		server.Dispose();
	}
}
