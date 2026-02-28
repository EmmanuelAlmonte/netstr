using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Netstr.Messaging.WebSockets;
using Netstr.Options;
using Netstr.RelayInformation;
using System.Text.Json;

namespace Netstr.Middleware
{
    /// <summary>
    /// Accepts websocket connections and starts listening to messages.
    /// </summary>
    public class WebSocketsMiddleware
    {
        private readonly IOptions<ConnectionOptions> options;
        private readonly ILogger<WebSocketsMiddleware> logger;
        private readonly WebSocketAdapterFactory factory;
        private readonly RequestDelegate next;

        public WebSocketsMiddleware(
            IOptions<ConnectionOptions> options,
            ILogger<WebSocketsMiddleware> logger,
            WebSocketAdapterFactory factory,
            RequestDelegate next)
        {
            this.options = options;
            this.logger = logger;
            this.factory = factory;
            this.next = next;
        }

        public async Task Invoke(HttpContext context, IRelayInformationService relayInformationService)
        {
            var webSocketsPath = ToPath(this.options.Value.WebSocketsPath);

            if (context.Request.Path == webSocketsPath)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    this.logger.LogInformation($"Accepting websocket connection from {context.Connection.RemoteIpAddress}");

                    var ws = await context.WebSockets.AcceptWebSocketAsync();
                    var adapter = this.factory.CreateAdapter(ws, context.Request.Headers, context.Connection);

                    await adapter.StartAsync();

                    this.logger.LogInformation($"Closing websocket connection from {context.Connection.RemoteIpAddress}");
                    this.factory.DisposeAdapter(adapter.Context.ClientId);
                    return;
                }

                if (HttpMethods.IsGet(context.Request.Method) && IsMetadataRequest(context.Request.Headers))
                {
                    EnsureRequiredCorsHeaders(context.Response.Headers);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/nostr+json";

                    var payload = JsonSerializer.Serialize(relayInformationService.GetDocument());
                    await context.Response.WriteAsync(payload);
                    return;
                }
            }

            await this.next(context);
        }

        private static bool IsMetadataRequest(IHeaderDictionary requestHeaders)
        {
            if (!requestHeaders.TryGetValue(HeaderNames.Accept, out var accepts) ||
                !MediaTypeHeaderValue.TryParseList(accepts, out var mediaTypes))
            {
                return false;
            }

            foreach (var mediaType in mediaTypes)
            {
                if (mediaType.MediaType.HasValue &&
                    string.Equals(mediaType.MediaType.Value, "application/nostr+json", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static PathString ToPath(string path)
        {
            if (path.StartsWith('/'))
            {
                return new PathString(path);
            }

            return new PathString($"/{path}");
        }

        private static void EnsureRequiredCorsHeaders(IHeaderDictionary responseHeaders)
        {
            if (!responseHeaders.ContainsKey(HeaderNames.AccessControlAllowOrigin))
            {
                responseHeaders[HeaderNames.AccessControlAllowOrigin] = "*";
            }

            if (!responseHeaders.ContainsKey(HeaderNames.AccessControlAllowHeaders))
            {
                responseHeaders[HeaderNames.AccessControlAllowHeaders] = "*";
            }

            if (!responseHeaders.ContainsKey(HeaderNames.AccessControlAllowMethods))
            {
                responseHeaders[HeaderNames.AccessControlAllowMethods] = "GET, OPTIONS";
            }
        }
    }
}
