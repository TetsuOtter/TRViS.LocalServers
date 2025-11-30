using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using TR.SimpleHttpServer;
using TR.SimpleHttpServer.WebSocket;

namespace TRViS.LocalServers.Core.WebSocket;

/// <summary>
/// WebSocket リクエストの処理を行うクラス
/// TR.SimpleHttpServer v1.1.0 の WebSocket サポートを使用
/// </summary>
public class WebSocketRequestHandler(WebSocketCore core)
{
	private readonly WebSocketCore core = core;
	private const int SYNC_DATA_INTERVAL_MS = 250; // 4回/秒
	private bool previousScenarioLoaded = false;

	private static readonly JsonSerializerOptions DeserializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	/// <summary>
	/// WebSocket リクエストを処理
	/// </summary>
	public async Task HandleWebSocketAsync(HttpRequest request, WebSocketConnection connection)
	{
		string clientId = core.CreateClientState();
		Console.WriteLine($"WebSocket client connected: {clientId}");

		// Send initial Timetable message when connection is established
		try
		{
			var timetableMessage = core.GenerateInitialTimetableMessage();
			if (timetableMessage != null)
			{
				string json = core.SerializeMessage(timetableMessage);
				await connection.SendTextAsync(json, CancellationToken.None);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error sending initial Timetable to {clientId}: {ex.Message}");
		}

		try
		{
			// クライアントからのメッセージ受信タスク
			var receiveTask = ReceiveMessagesAsync(clientId, connection);

			// 定期的に SyncedData を送信するタスク
			var syncTask = SendSyncDataAsync(clientId, connection);

			// どちらかが完了するまで待機
			await Task.WhenAny(receiveTask, syncTask);

			// 接続をクローズ
			if (connection.IsOpen)
			{
				await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", CancellationToken.None);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"WebSocket handling error: {ex.Message}");
		}
		finally
		{
			Console.WriteLine($"WebSocket client disconnected: {clientId}");
			core.UnregisterWebSocket(clientId);
		}
	}

	/// <summary>
	/// クライアントからのメッセージを受信して処理
	/// </summary>
	private async Task ReceiveMessagesAsync(string clientId, WebSocketConnection connection)
	{
		try
		{
			while (connection.IsOpen)
			{
				var message = await connection.ReceiveMessageAsync(CancellationToken.None);

				if (message.Type == WebSocketMessageType.Close)
				{
					break;
				}

				if (message.Type == WebSocketMessageType.Text)
				{
					string text = message.GetText();
					await ProcessMessageAsync(clientId, text, connection);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error receiving WebSocket messages: {ex.Message}");
		}
	}

	/// <summary>
	/// 定期的に SyncedData を送信
	/// </summary>
	private async Task SendSyncDataAsync(string clientId, WebSocketConnection connection)
	{
		try
		{
			while (connection.IsOpen)
			{
				// シナリオ読み込み状態の変化を検知
				bool currentScenarioLoaded = core.Bridge.IsScenarioLoaded;
				if (previousScenarioLoaded != currentScenarioLoaded)
				{
					previousScenarioLoaded = currentScenarioLoaded;
					if (currentScenarioLoaded)
					{
						Console.WriteLine("[TimetableServerPlugin] Scenario loaded.");
						// 新しいシナリオが読み込まれた場合、初期 Timetable メッセージを送信
						try
						{
							var timetableMessage = core.GenerateInitialTimetableMessage();
							if (timetableMessage != null)
							{
								string json = core.SerializeMessage(timetableMessage);
								await connection.SendTextAsync(json, CancellationToken.None);
								Console.WriteLine($"Scenario changed, sent initial Timetable to {clientId}");
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Error sending initial Timetable to {clientId}: {ex.Message}");
						}
					}
				}

				var syncedDataMessage = core.GenerateSyncedDataMessage();
				if (syncedDataMessage != null)
				{
					string syncJson = core.SerializeMessage(syncedDataMessage);
					await connection.SendTextAsync(syncJson, CancellationToken.None);

					// 送信したデータの時刻から次の送信タイミングを計算
					long sentSimulatorTimeMs = syncedDataMessage.Time_ms;
					int delayMs = SYNC_DATA_INTERVAL_MS;

					// シミュレータ時間で次の秒になるまでの時間を計算
					int millisecondsUntilNextSecond = 1000 - (int)(sentSimulatorTimeMs % 1000);

					// 待機時間内に次の秒になる場合は、その秒+1msだけ待機
					if (millisecondsUntilNextSecond <= delayMs)
					{
						delayMs = millisecondsUntilNextSecond + 1;
					}

					await Task.Delay(delayMs);
				}
				else
				{
					// メッセージ生成に失敗した場合は基本間隔で待機
					await Task.Delay(SYNC_DATA_INTERVAL_MS);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error sending SyncedData: {ex.Message}");
		}
	}

	/// <summary>
	/// 受信したメッセージを処理
	/// </summary>
	private async Task ProcessMessageAsync(string clientId, string messageJson, WebSocketConnection connection)
	{
		try
		{
			// JSON を解析して MessageType があるかチェック
			using var doc = JsonDocument.Parse(messageJson);
			var root = doc.RootElement;

			// MessageType フィールドがあるかチェック
			if (root.TryGetProperty("MessageType", out var messageTypeElement))
			{
				// Timetable または SyncedData メッセージと考える
				// ここではクライアントから Timetable/SyncedData は通常送信されないため無視
				return;
			}

			// MessageType がない場合は ID 更新メッセージと解釈
			var idUpdateMessage = JsonSerializer.Deserialize<ClientIdUpdateMessage>(messageJson, DeserializerOptions);
			if (idUpdateMessage != null)
			{
				core.HandleClientIdUpdate(clientId, idUpdateMessage);

				// 更新後、クライアントに timetable を送信
				var state = core.GetClientState(clientId);
				if (state != null)
				{
					var timetableMessage = core.GenerateTimetableMessage(
						state.WorkGroupId,
						state.WorkId,
						state.TrainId
					);

					if (timetableMessage != null)
					{
						string responseJson = core.SerializeMessage(timetableMessage);
						await connection.SendTextAsync(responseJson, CancellationToken.None);
					}
				}
			}
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"JSON parse error: {ex.Message}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Message processing error: {ex.Message}");
		}
	}
}
