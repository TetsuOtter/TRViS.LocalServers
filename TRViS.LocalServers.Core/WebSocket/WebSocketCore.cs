using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

using TRViS.JsonModels;

namespace TRViS.LocalServers.Core.WebSocket;

/// <summary>
/// WebSocket 接続の管理と通信処理を行うコアクラス
/// </summary>
public class WebSocketCore(ITimetableServerBridge bridge) : IDisposable
{
	private readonly ITimetableServerBridge bridge = bridge;

	/// <summary>
	/// Expose the timetable bridge so callers (request handlers) can subscribe to its events.
	/// </summary>
	public ITimetableServerBridge Bridge => bridge;
	private readonly ConcurrentDictionary<string, WebSocketClientState> clientStates = new();

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	/// <summary>
	/// 新しい WebSocket クライアントの状態を作成
	/// </summary>
	public string CreateClientState()
	{
		string clientId = Guid.NewGuid().ToString();
		clientStates[clientId] = new WebSocketClientState();
		return clientId;
	}

	/// <summary>
	/// クライアント状態を削除
	/// </summary>
	public void UnregisterWebSocket(string clientId)
	{
		clientStates.TryRemove(clientId, out _);
	}

	/// <summary>
	/// クライアントからの ID 更新メッセージを処理
	/// </summary>
	public void HandleClientIdUpdate(string clientId, ClientIdUpdateMessage message)
	{
		if (clientStates.TryGetValue(clientId, out var state))
		{
			if (!string.IsNullOrEmpty(message.WorkGroupId))
				state.WorkGroupId = message.WorkGroupId;
			if (!string.IsNullOrEmpty(message.WorkId))
				state.WorkId = message.WorkId;
			if (!string.IsNullOrEmpty(message.TrainId))
				state.TrainId = message.TrainId;
		}
	}

	/// <summary>
	/// SyncedData メッセージを生成
	/// </summary>
	public ServerSyncedDataMessage? GenerateSyncedDataMessage()
	{
		SyncedData? syncedData = bridge.GetSyncedData();
		if (syncedData is null)
			return null;

		return new ServerSyncedDataMessage
		{
			Location_m = syncedData.Location_m,
			Time_ms = syncedData.Time_ms ?? 0,
			CanStart = syncedData.CanStart ?? false
		};
	}

	/// <summary>
	/// Timetable メッセージを生成
	/// </summary>
	public ServerTimetableMessage? GenerateTimetableMessage(string? workGroupId, string? workId, string? trainId)
	{
		WorkGroupData[]? data = null;

		// スコープに応じてデータを取得
		if (!string.IsNullOrEmpty(trainId))
		{
			data = bridge.GetWorkGroupByTrainId(trainId);
		}
		else if (!string.IsNullOrEmpty(workId))
		{
			data = bridge.GetWorkGroupByWorkId(workId);
		}
		else if (!string.IsNullOrEmpty(workGroupId))
		{
			data = bridge.GetWorkGroupByWorkGroupId(workGroupId);
		}

		if (data is null)
			return null;

		var message = new ServerTimetableMessage { Data = data };

		// スコープに応じて ID を設定
		if (!string.IsNullOrEmpty(trainId))
			message.TrainId = trainId;
		else if (!string.IsNullOrEmpty(workId))
			message.WorkId = workId;
		else if (!string.IsNullOrEmpty(workGroupId))
			message.WorkGroupId = workGroupId;

		return message;
	}

	/// <summary>
	/// 初回接続時用の Timetable メッセージを生成（すべてのデータを返す）
	/// </summary>
	public ServerTimetableMessage? GenerateInitialTimetableMessage()
	{
		WorkGroupData[]? data = bridge.GetWorkGroup();

		if (data is null)
			return null;

		var message = new ServerTimetableMessage { Data = data };
		return message;
	}

	/// <summary>
	/// メッセージを JSON 文字列にシリアライズ
	/// </summary>
	public string SerializeMessage(object message)
	{
		return JsonSerializer.Serialize(message, SerializerOptions);
	}

	/// <summary>
	/// クライアントの状態を取得
	/// </summary>
	public WebSocketClientState? GetClientState(string clientId)
	{
		clientStates.TryGetValue(clientId, out var state);
		return state;
	}

	public void Dispose()
	{
		clientStates.Clear();
	}
}
