namespace Netstr.Options
{
    public record AuthOptions
    {
        public AuthMode Mode { get; init; }

        public long[] ProtectedKinds { get; init; } = [];

        public int AuthCreatedAtWindowSeconds { get; init; } = 600;
    }
}
