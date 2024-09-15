using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TR.SimpleHttpServer;

using TRViS.JsonModels;

namespace TRViS.LocalServers.Core;

public class TimetableServerCore : IDisposable
{
	const string LISTENER_PATH = "/";
	const string JSON_FILE_MIME = "application/json";
	const string TIMETABLE_FILE_MIME = JSON_FILE_MIME;
	const string TIMETABLE_FILE_NAME = "timetable.json";
	const string SYNC_SERVICE_PATH = "sync";
	const string QR_HTML_FILE_NAME = "index.html";
	const string SCENARIO_INFO_FILE_NAME = "scenario-info.json";
	const int LISTENER_PORT = 58600;
	const int PORT_RETRY_MAX = 10;
	public IPAddress? ipv4Address;
	public readonly int port;
	readonly HttpServer server;

	readonly NameValueCollection additionalHeaders = [];

	public static readonly Assembly currentAssembly = Assembly.GetExecutingAssembly();
	readonly ITimetableServerBridge bridge;

	public TimetableServerCore(ITimetableServerBridge bridge)
	{
		additionalHeaders.Add("Access-Control-Allow-Origin", "*");
		ipv4Address = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(v => v.AddressFamily == AddressFamily.InterNetwork);
		this.bridge = bridge;
		server = StartListener(out port);
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
			LISTENER_PATH + SYNC_SERVICE_PATH => true,
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
		else if (pathWithoutQueryOrHash is LISTENER_PATH + SYNC_SERVICE_PATH)
		{
			return new(
				status: "200 OK",
				ContentType: JSON_FILE_MIME,
				additionalHeaders: additionalHeaders,
				body: GenerateSyncResponse()
			);
		}

		if (!bridge.IsScenarioLoaded)
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
		ResponseTypes.ScenarioInfo? scenarioInfo = bridge.CurrentScenario;
		if (scenarioInfo is null)
		{
			return [];
		}

		return JsonSerializer.SerializeToUtf8Bytes(
			scenarioInfo,
			GenerateScenarioInfo_JsonSerializerOptions
		);
	}

	static readonly JsonSerializerOptions GenerateJson_JsonSerializerOptions = new()
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};
	byte[] GenerateJson()
	{
		WorkGroupData[]? arr = bridge.GetWorkGroup();
		if (arr is null)
		{
			return [];
		}

		return JsonSerializer.SerializeToUtf8Bytes(arr, GenerateJson_JsonSerializerOptions);
	}

	byte[] GenerateSyncResponse()
	{
		SyncedData? syncedData = bridge.GetSyncedData();
		if (syncedData is null)
		{
			return [];
		}

		return JsonSerializer.SerializeToUtf8Bytes(syncedData, GenerateJson_JsonSerializerOptions);
	}

	private static async Task<HttpResponse> GenResponseFromEmbeddedResourceAsync(string fileName, string contentType, NameValueCollection additionalHeaders)
	{
		using Stream stream = currentAssembly.GetManifestResourceStream($"TRViS.LocalServers.Core.{fileName}");
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

	public string BrowserLink => $"http://localhost:{port}{LISTENER_PATH}{QR_HTML_FILE_NAME}?host={ipv4Address}&port={port}";
	public void OnOpenBrowserClicked() => Process.Start(BrowserLink);

	public void Dispose()
	{
		server.Stop();
		server.Dispose();
	}
}
