using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using BveEx.PluginHost.Plugins;
using BveEx.PluginHost.Plugins.Extensions;

using BveTypes.ClassWrappers;

using TRViS.JsonModels;
using TRViS.LocalServers.Core;

namespace TRViS.LocalServers.BveTs;

[Plugin(PluginType.Extension)]
public partial class TimetableServerPlugin : PluginBase, IExtension, ITimetableServerBridge
{
	const string WORK_GROUP_ID = "1";
	const string WORK_ID = "1-1";
	const string TRAIN_ID = "1-1-1";
	readonly TimetableServerCore server;

	public TimetableServerPlugin(PluginBuilder builder) : base(builder)
	{
		server = new(this);

		AddLaunchBrowserButtonToContextMenu();
	}

	public bool IsScenarioLoaded => BveHacker.IsScenarioCreated;
	public ResponseTypes.ScenarioInfo? CurrentScenario => new(
			routeName: BveHacker.ScenarioInfo.RouteTitle,
			scenarioName: BveHacker.ScenarioInfo.Title,
			carName: BveHacker.ScenarioInfo.VehicleTitle
		);

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
	private static readonly Regex beginWithSpeedLimitRegex = new(@"^([\d-]+)\s*\/(.+)$", RegexOptions.Compiled);
	private static readonly Regex endWithSpeedLimitRegex = new(@"^(.+)\/\s*([\d-]+)$", RegexOptions.Compiled);
	public WorkGroupData[]? GetWorkGroup()
	{
		if (!BveHacker.IsScenarioCreated)
			return null;

		Scenario scenario = BveHacker.Scenario;
		ScenarioInfo scenarioInfo = BveHacker.ScenarioInfo;
		TimeTable timeTable = scenario.TimeTable;

		Station[] stationArray = scenario.Route.Stations.Cast<Station>().ToArray();
		TimetableRowData[] trvisTimetableRows = new TimetableRowData[stationArray.Length];
		// 最後の駅以外ドアが開かない場合、その列車は非営業列車である -> ドアが開かない駅でも運転停車ではない
		bool isAllStationDoorNotOpenExceptLastStation = stationArray
			.Take(stationArray.Length - 1)
			.All(station => station.Pass || station.DoorSide == 0);
		double motorCarCount = scenario.Vehicle.Dynamics.MotorCar.Count;
		double trailerCarCount = scenario.Vehicle.Dynamics.TrailerCar.Count;
		double trainLength = (motorCarCount + trailerCarCount) * scenario.Vehicle.Dynamics.CarLength;
		// 多少の誤差を考慮し、編成が入りきるよりも少し長めに設定
		double onStationDetectRadius_m = (trainLength / 2) + 50;
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
			string departureStr = timeTable.DepartureTimeTexts[indexOfTimetableInstance];
			trvisTimetableRows[i] = new TimetableRowData(
				Id: $"{TRAIN_ID}-{i}",
				StationName: staName,
				Location_m: station.Location - trainLength / 2,
				Longitude_deg: null,
				Latitude_deg: null,
				OnStationDetectRadius_m: onStationDetectRadius_m,
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

		string motorCarCountStr = 0 < motorCarCount ? $"{motorCarCount:#.#}M" : string.Empty;
		string trailerCarCountStr = 0 < trailerCarCount ? $"{trailerCarCount:#.#}T" : string.Empty;
		TrainData trvisTrainData = new(
			Id: TRAIN_ID,
			TrainNumber: scenarioInfo.Title,
			MaxSpeed: null,
			SpeedType: null,
			NominalTractiveCapacity: $"{scenarioInfo.VehicleTitle}\n{motorCarCountStr}{trailerCarCountStr}",
			CarCount: (int)Math.Ceiling(motorCarCount + trailerCarCount),
			Destination: null,
			BeginRemarks: null,
			AfterRemarks: null,
			Remarks: "Generated with TRViS Local Servers (BveEX Extension)\n" + scenarioInfo.Comment,
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
			Id: WORK_ID,
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
			Id: WORK_GROUP_ID,
			Name: "TRViS Local Servers (BveEX Extension)",
			DBVersion: 1,
			Works: [trvisWorkData]
		);

		return [trvisWorkGroupData];
	}

	public WorkGroupData[]? GetWorkGroupByWorkGroupId(string workGroupId) => TimetableServerBridgeUtils.GetWorkGroupByWorkGroupId(GetWorkGroup, workGroupId);
	public WorkGroupData[]? GetWorkGroupByWorkId(string workId) => TimetableServerBridgeUtils.GetWorkGroupByWorkId(GetWorkGroup, workId);
	public WorkGroupData[]? GetWorkGroupByTrainId(string trainId) => TimetableServerBridgeUtils.GetWorkGroupByTrainId(GetWorkGroup, trainId);

	public SyncedData GetSyncedData()
	{
		return BveHacker.IsScenarioCreated
			? new(
				Location_m: BveHacker.Scenario.LocationManager.Location,
				Time_ms: BveHacker.Scenario.TimeManager.TimeMilliseconds,
				CanStart: true
			)
			: new(
				Location_m: null,
				Time_ms: null,
				CanStart: false
			);
	}

	public override TickResult Tick(TimeSpan elapsed) => new ExtensionTickResult();

	public override void Dispose()
	{
		server.Dispose();
	}
}
