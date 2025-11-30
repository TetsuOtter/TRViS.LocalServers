using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TrainCrew;

using TRViS.JsonModels;
using TRViS.LocalServers.Core;
using TRViS.LocalServers.ResponseTypes;

namespace TRViS.LocalServers.TrainCrew;

public class TimetableServerTrainCrewBridge : ITimetableServerBridge, IDisposable
{
	const string WORK_GROUP_ID = "1";
	const string WORK_ID = "1-1";
	const float STA_LOCATION_ADJUST_M = 100;
	static readonly TimeSpan STA_LIST_REQUEST_INTERVAL = TimeSpan.FromSeconds(5);

	public bool IsScenarioLoaded => TrainCrewInput.gameState.gameScreen is GameScreen.MainGame or GameScreen.MainGame_Pause or GameScreen.MainGame_Loading;

	private TrainState _state = new();
	public ScenarioInfo? CurrentScenario => new(
		"TRAIN CREW",
		_state.diaName,
		GetCarInfoString()
	);

	private string GetCarInfoString()
	{
		if (!IsScenarioLoaded)
		{
			return string.Empty;
		}

		List<CarState> carStateList = _state.CarStates;
		if (carStateList.Count == 0)
		{
			return string.Empty;
		}

		string ret = "";
		string carModel = carStateList[0].CarModel;
		int mCar = 0;
		int tCar = 0;
		void appendToRet()
		{
			if (ret.Length != 0)
			{
				ret += "\n";
			}
			ret += $"{carModel}　{mCar}Ｍ{tCar}Ｔ";
		}
		foreach (CarState carState in carStateList)
		{
			if (carState.CarModel != carModel)
			{
				appendToRet();
				carModel = carState.CarModel;
				mCar = 0;
				tCar = 0;
			}

			if (carState.HasMotor)
			{
				++mCar;
			}
			else
			{
				++tCar;
			}
		}
		if (mCar != 0 || tCar != 0)
		{
			appendToRet();
		}

		return ret;
	}

	public SyncedData GetSyncedData()
	{
		if (!IsScenarioLoaded)
		{
			return new(
				null,
				null,
				false
			);
		}

		TrainState state = _state;
		return new(
			state.TotalLength,
			// もしかしたら25時になるかもしれない
			(long)state.NowTime.TotalMilliseconds,
			true
		);
	}
	public WorkGroupData[]? GetWorkGroup()
	{
		if (!IsScenarioLoaded)
		{
			return null;
		}

		TrainState state = _state;
		int carLength_m = state.CarStates.Count * 20;
		TimetableRowData[] timetableRowArr = new TimetableRowData[state.stationList.Count];
		string? trainDestination = state.BoundFor;
		for (int i = 0; i < timetableRowArr.Length; i++)
		{
			Station station = state.stationList[i];
			bool isPass = station.stopType == StopType.Passing;
			bool isLastStop = i == timetableRowArr.Length - 1;
			string? trackName = station.StopPosName.Split(["駅", "検車区", "信号場"], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
			if (trackName is not null)
			{
				if (trackName.EndsWith("上り") || trackName.EndsWith("下り"))
				{
					if (2 < trackName.Length)
					{
						string trackNameMain = trackName.Substring(0, trackName.Length - 2);
						if (trackNameMain.EndsWith("番"))
						{
							trackNameMain = trackNameMain.Substring(0, trackNameMain.Length - 1);
						}
						trackName = trackNameMain + "\n" + trackName.Substring(trackName.Length - 2);
					}
				}
			}
			else
			{
				if (trackName?.EndsWith("番") == true)
				{
					trackName = trackName.Substring(0, trackName.Length - 1);
				}
			}

			string stationName = station.Name;
			if (stationName.EndsWith("信号場"))
			{
				stationName = stationName.Substring(0, stationName.Length - 2);
			}
			timetableRowArr[i] = new(
				Id: i.ToString(),
				StationName: stationName,
				// ゲーム開始位置からの距離のため、上下の考慮は不要
				Location_m: station.TotalLength - (carLength_m / 2),
				Longitude_deg: null,
				Latitude_deg: null,
				OnStationDetectRadius_m: carLength_m / 2 + STA_LOCATION_ADJUST_M,
				FullName: null,
				RecordType: null,
				TrackName: trackName,
				DriveTime_MM: null,
				DriveTime_SS: null,
				IsOperationOnlyStop: station.stopType == StopType.StopForOperation,
				IsPass: isPass,
				HasBracket: false,
				IsLastStop: isLastStop,
				Arrive: i == 0 ? null : isPass ? "↓" : getTimeStr(station.ArvTime),
				Departure: isLastStop ? null : getTimeStr(station.DepTime),
				RunInLimit: null,
				RunOutLimit: null,
				Remarks: null,
				MarkerColor: null,
				MarkerText: null,
				WorkType: null
			);

			if (isLastStop && station.Name == trainDestination)
			{
				trainDestination = null;
			}
		}

		TrainData trainData = new(
			// 本当は固定でも大丈夫なはずだが、TRViSのバグで正常に更新されないため、列番を使用する
			Id: state.diaName,
			TrainNumber: state.diaName,
			MaxSpeed: null,
			SpeedType: null,
			NominalTractiveCapacity: GetCarInfoString(),
			CarCount: state.CarStates.Count,
			Destination: trainDestination,
			BeginRemarks: null,
			AfterRemarks: null,
			Remarks: null,
			BeforeDeparture: null,
			TrainInfo: state.Class,
			Direction: 1,
			WorkType: null,
			AfterArrive: null,
			BeforeDeparture_OnStationTrackCol: null,
			AfterArrive_OnStationTrackCol: null,
			DayCount: 0,
			IsRideOnMoving: false,
			Color: null,
			TimetableRows: timetableRowArr
		);

		WorkData workData = new(
			Id: WORK_ID,
			Name: state.diaName,
			AffectDate: null,
			AffixContentType: null,
			AffixContent: null,
			Remarks: "TRAIN CREW向けに自動生成されたデータです。\n"
			+ $"TRViS.LocalServers.Core: ${TimetableServerCore.currentAssembly.FullName}\n"
			+ $"TRViS.LocalServers.TrainCrew: ${GetType().Assembly.FullName}",
			HasETrainTimetable: false,
			ETrainTimetableContentType: null,
			ETrainTimetableContent: null,
			Trains: [trainData]
		);

		WorkGroupData workGroupData = new(
			Id: WORK_GROUP_ID,
			Name: "TRViS Local Servers (for TRAIN CREW)",
			DBVersion: 1,
			Works: [workData]
		);

		return [workGroupData];
	}

	static string getTimeStr(in TimeSpan time) => time.ToString(@"hh\:mm\:ss");
	readonly CancellationTokenSource loopCancel = new();

	private string? lastTrainId = null;

	public event EventHandler<TrainChangedEventArgs>? OnTrainChanged;

	public TimetableServerTrainCrewBridge()
	{
		// 100msごとに処理するタスクを実行する
		Task.Run(async () =>
		{
			CancellationToken token = loopCancel.Token;
			try
			{
				while (!token.IsCancellationRequested)
				{
					Tick();
					await Task.Delay(100, token);
				}
			}
			catch (TaskCanceledException) { }
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				Console.WriteLine("[TimetableServerTrainCrewBridge] Unexpected Error: {0}", ex);
			}
		});
	}

	DateTime lastStaListRequest = DateTime.MinValue;
	bool lastScenarioLoadedState = false;
	void Tick()
	{
		_state = TrainCrewInput.GetTrainState();

		// detect train identity change and raise event
		try
		{
			string? currentTrainId = _state?.diaName;
			if (currentTrainId != lastTrainId)
			{
				lastTrainId = currentTrainId;
				OnTrainChanged?.Invoke(this, new TrainChangedEventArgs { TrainId = currentTrainId });
			}
		}
		catch (Exception) { }

		// すぐに返ってくるわけではないはずなので、次のループでの取得を期待してリクエストする
		DateTime now = DateTime.Now;
		if (lastScenarioLoadedState != IsScenarioLoaded)
		{
			// ロード直後に駅リストを更新するため
			lastScenarioLoadedState = IsScenarioLoaded;
			_state?.stationList.Clear();
			if (IsScenarioLoaded)
			{
				lastStaListRequest = now;
				TrainCrewInput.RequestStaData();
			}
		}
		else if (IsScenarioLoaded && STA_LIST_REQUEST_INTERVAL < now - lastStaListRequest)
		{
			lastStaListRequest = now;
			TrainCrewInput.RequestStaData();
		}
	}

	public void Dispose()
	{
		loopCancel.Cancel();
		loopCancel.Dispose();
	}

	public WorkGroupData[]? GetWorkGroupByWorkGroupId(string workGroupId) => TimetableServerBridgeUtils.GetWorkGroupByWorkGroupId(GetWorkGroup, workGroupId);
	public WorkGroupData[]? GetWorkGroupByWorkId(string workId) => TimetableServerBridgeUtils.GetWorkGroupByWorkId(GetWorkGroup, workId);
	public WorkGroupData[]? GetWorkGroupByTrainId(string trainId) => TimetableServerBridgeUtils.GetWorkGroupByTrainId(GetWorkGroup, trainId);
}
