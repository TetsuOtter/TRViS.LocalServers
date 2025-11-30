using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

using TR.SimpleHttpServer;
using TR.SimpleHttpServer.WebSocket;

using TRViS.LocalServers.Core.Http;
using TRViS.LocalServers.Core.WebSocket;

namespace TRViS.LocalServers.Core;

public class TimetableServerCore : IDisposable
{
	const string LISTENER_PATH = "/";
	const string WEBSOCKET_PATH = "/ws";
	const int LISTENER_PORT = 58600;
	const int PORT_RETRY_MAX = 10;
	public IPAddress[] ipv4Addresses;
	public readonly int port;
	readonly HttpServer server;

	readonly NameValueCollection additionalHeaders = [];

	public static readonly Assembly currentAssembly = Assembly.GetExecutingAssembly();
	readonly HttpRequestHandler httpRequestHandler;
	readonly WebSocketRequestHandler webSocketRequestHandler;
	readonly WebSocketCore webSocketCore;

	public TimetableServerCore(ITimetableServerBridge bridge)
	{
		additionalHeaders.Add("Access-Control-Allow-Origin", "*");
		ipv4Addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(v => v.AddressFamily == AddressFamily.InterNetwork).ToArray();
		httpRequestHandler = new HttpRequestHandler(bridge, additionalHeaders);
		webSocketCore = new WebSocketCore(bridge);
		webSocketRequestHandler = new WebSocketRequestHandler(webSocketCore);
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
				server = new((ushort)port, HttpHandlerAsync, HandleWebSocketPath);
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

	private Task<WebSocketHandler?> HandleWebSocketPath(string path)
	{
		if (path == WEBSOCKET_PATH)
		{
			return Task.FromResult<WebSocketHandler?>(
				webSocketRequestHandler.HandleWebSocketAsync
			);
		}
		return Task.FromResult<WebSocketHandler?>(null);
	}

	private async Task<HttpResponse> HttpHandlerAsync(HttpRequest request)
	{
		try
		{
			return await httpRequestHandler.HandleRequestAsync(request);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return new HttpResponse(
				status: "500 Internal Server Error",
				ContentType: "text/plain",
				additionalHeaders: additionalHeaders,
				body: $"Internal Server Error: {ex.Message}\r\n{ex.StackTrace}"
			);
		}
	}

	public string BrowserLinkPath => $"{LISTENER_PATH}index.html?host={string.Join(",", ipv4Addresses.Select(a => a.ToString()))}&port={port}";
	public string BrowserLink => $"http://localhost:{port}{BrowserLinkPath}";
	public void OnOpenBrowserClicked() => Process.Start(BrowserLink);

	public void Dispose()
	{
		server.Stop();
		server.Dispose();
		webSocketCore.Dispose();
	}
}
