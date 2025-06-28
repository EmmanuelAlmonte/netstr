namespace Netstr.Messaging.Models.Nip05
{
    /// <summary>
    /// Result of NIP-05 verification attempt
    /// </summary>
    public class Nip05Result
    {
        public bool IsValid { get; }
        public string? Error { get; }
        public DateTime VerifiedAt { get; }
        
        private Nip05Result(bool isValid, string? error = null)
        {
            IsValid = isValid;
            Error = error;
            VerifiedAt = DateTime.UtcNow;
        }
        
        public static Nip05Result Valid() => new(true);
        public static Nip05Result Invalid(string error) => new(false, error);
    }
}