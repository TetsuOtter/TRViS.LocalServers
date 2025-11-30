using System.Text.Json.Serialization;

namespace TRViS.LocalServers.Core.WebSocket;

/// <summary>
/// クライアントから送信される ID 更新メッセージ
/// </summary>
public class ClientIdUpdateMessage
{
  [JsonPropertyName("WorkGroupId")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WorkGroupId { get; set; }

  [JsonPropertyName("WorkId")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WorkId { get; set; }

  [JsonPropertyName("TrainId")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? TrainId { get; set; }
}

/// <summary>
/// サーバーから送信される SyncedData メッセージ
/// </summary>
public class ServerSyncedDataMessage
{
  [JsonPropertyName("MessageType")]
  public string MessageType { get; } = "SyncedData";

  [JsonPropertyName("Location_m")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public double? Location_m { get; set; }

  [JsonPropertyName("Time_ms")]
  public long Time_ms { get; set; }

  [JsonPropertyName("CanStart")]
  public bool CanStart { get; set; }
}

/// <summary>
/// サーバーから送信される Timetable メッセージ
/// </summary>
public class ServerTimetableMessage
{
  [JsonPropertyName("MessageType")]
  public string MessageType { get; } = "Timetable";

  [JsonPropertyName("WorkGroupId")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WorkGroupId { get; set; }

  [JsonPropertyName("WorkId")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WorkId { get; set; }

  [JsonPropertyName("TrainId")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? TrainId { get; set; }

  [JsonPropertyName("Data")]
  public object? Data { get; set; }
}

/// <summary>
/// WebSocket クライアント接続を管理するための情報
/// </summary>
public class WebSocketClientState
{
  public string? WorkGroupId { get; set; }
  public string? WorkId { get; set; }
  public string? TrainId { get; set; }
  public long LastSyncedDataTime_ms { get; set; } = -1;
}
