using System;
using System.Linq;

using TRViS.JsonModels;

namespace TRViS.LocalServers.Core;

public interface ITimetableServerBridge
{
	bool IsScenarioLoaded { get; }
	ResponseTypes.ScenarioInfo? CurrentScenario { get; }

	SyncedData GetSyncedData();

	WorkGroupData[]? GetWorkGroup();
	WorkGroupData[]? GetWorkGroupByWorkGroupId(string workGroupId);
	WorkGroupData[]? GetWorkGroupByWorkId(string workId);
	WorkGroupData[]? GetWorkGroupByTrainId(string trainId);
}

public static class TimetableServerBridgeUtils
{
	public static WorkGroupData[]? GetWorkGroupByWorkGroupId(Func<WorkGroupData[]?> getWorkGroup, string workGroupId)
	{
		if (string.IsNullOrEmpty(workGroupId))
		{
			return null;
		}

		WorkGroupData[]? arr = getWorkGroup();
		if (arr is null)
		{
			return null;
		}

		arr = arr.Where(wg => wg.Id == workGroupId).ToArray();
		if (arr.Length == 0)
		{
			return null;
		}

		return arr;
	}
	public static WorkGroupData[]? GetWorkGroupByWorkId(Func<WorkGroupData[]?> getWorkGroup, string workId)
	{
		if (string.IsNullOrEmpty(workId))
		{
			return null;
		}

		WorkGroupData[]? arr = getWorkGroup();
		if (arr is null)
		{
			return null;
		}

		arr = arr.Where(wg => wg.Works.Any(w => w.Id == workId)).ToArray();
		if (arr.Length == 0)
		{
			return null;
		}

		return arr;
	}
	public static WorkGroupData[]? GetWorkGroupByTrainId(Func<WorkGroupData[]?> getWorkGroup, string trainId)
	{
		if (string.IsNullOrEmpty(trainId))
		{
			return null;
		}

		WorkGroupData[]? arr = getWorkGroup();
		if (arr is null)
		{
			return null;
		}

		arr = arr.Where(wg => wg.Works.Any(w => w.Trains.Any(t => t.Id == trainId))).ToArray();
		if (arr.Length == 0)
		{
			return null;
		}

		return arr;
	}
}
