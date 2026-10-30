namespace Netstr.Options
{
    public class ConnectionOptions
    {
        public required string WebSocketsPath { get; init; }
        public bool UseHttpsRedirection { get; init; } = true;
        public bool UseForwardedHeaders { get; init; } = true;
    }
}
