using System;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TR.SimpleHttpServer;

using TRViS.JsonModels;

namespace TRViS.LocalServers.Core.Http;

/// <summary>
/// HTTP リクエストの処理を行うクラス
/// </summary>
public class HttpRequestHandler(ITimetableServerBridge bridge, NameValueCollection additionalHeaders)
{
	const string LISTENER_PATH = "/";
	const string JSON_FILE_MIME = "application/json";
	const string TIMETABLE_FILE_MIME = JSON_FILE_MIME;
	const string TIMETABLE_FILE_NAME = "timetable.json";
	const string SCENARIO_INFO_FILE_NAME = "scenario-info.json";
	const string QR_HTML_FILE_NAME = "index.html";
	const string SYNC_SERVICE_PATH = "sync";

	private readonly ITimetableServerBridge bridge = bridge;
	private readonly NameValueCollection additionalHeaders = additionalHeaders;

	private static readonly JsonSerializerOptions JsonSerializerOptions = new()
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private static readonly JsonSerializerOptions ScenarioInfoSerializerOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	private static bool IsPathAcceptable(string path)
		=> path switch
		{
			LISTENER_PATH or (LISTENER_PATH + QR_HTML_FILE_NAME) => true,
			LISTENER_PATH + TIMETABLE_FILE_NAME => true,
			LISTENER_PATH + SCENARIO_INFO_FILE_NAME => true,
			LISTENER_PATH + SYNC_SERVICE_PATH => true,
			_ => false
		};

	public async Task<HttpResponse> HandleRequestAsync(HttpRequest request)
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

		if (pathWithoutQueryOrHash is LISTENER_PATH)
		{
			return new HttpResponse(
				status: "302 Found",
				ContentType: "text/plain",
				additionalHeaders: new NameValueCollection
				{
					{ "Location", $"{LISTENER_PATH}{QR_HTML_FILE_NAME}" }
				},
				body: "Found"
			);
		}

		if (pathWithoutQueryOrHash is (LISTENER_PATH + QR_HTML_FILE_NAME))
		{
			return await GenResponseFromEmbeddedResourceAsync(QR_HTML_FILE_NAME, "text/html");
		}

		if (pathWithoutQueryOrHash is LISTENER_PATH + SYNC_SERVICE_PATH)
		{
			return new HttpResponse(
				status: "200 OK",
				ContentType: JSON_FILE_MIME,
				additionalHeaders: additionalHeaders,
				body: GenerateSyncResponse()
			);
		}

		if (!bridge.IsScenarioLoaded)
		{
			return new HttpResponse(
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
				_ => throw new InvalidOperationException("Invalid path.")
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

	private byte[] GenerateScenarioInfoJson()
	{
		ResponseTypes.ScenarioInfo? scenarioInfo = bridge.CurrentScenario;
		if (scenarioInfo is null)
		{
			return [];
		}

		return JsonSerializer.SerializeToUtf8Bytes(
			scenarioInfo,
			ScenarioInfoSerializerOptions
		);
	}

	private byte[] GenerateJson()
	{
		WorkGroupData[]? arr = bridge.GetWorkGroup();
		if (arr is null)
		{
			return [];
		}

		return JsonSerializer.SerializeToUtf8Bytes(arr, JsonSerializerOptions);
	}

	private byte[] GenerateSyncResponse()
	{
		SyncedData? syncedData = bridge.GetSyncedData();
		if (syncedData is null)
		{
			return [];
		}

		return JsonSerializer.SerializeToUtf8Bytes(syncedData, JsonSerializerOptions);
	}

	private async Task<HttpResponse> GenResponseFromEmbeddedResourceAsync(string fileName, string contentType)
	{
		var assembly = System.Reflection.Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream($"TRViS.LocalServers.Core.{fileName}");

		if (stream is null)
		{
			return new HttpResponse(
				status: "404 Not Found",
				ContentType: "text/plain",
				additionalHeaders: additionalHeaders,
				body: "Resource not found"
			);
		}

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
}
