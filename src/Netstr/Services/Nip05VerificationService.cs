using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Netstr.Messaging.Models.Nip05;

namespace Netstr.Services
{
    /// <summary>
    /// Service for verifying NIP-05 DNS-based identities
    /// </summary>
    public interface INip05VerificationService
    {
        Task<Nip05Result> VerifyIdentifierAsync(string identifier, string pubkey);
        Task<string?> GetVerifiedIdentifierAsync(string pubkey);
        Task<bool> IsIdentifierVerifiedAsync(string identifier, string pubkey);
    }

    public class Nip05VerificationService : INip05VerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<Nip05VerificationService> _logger;

        // Cache keys
        private const string CACHE_KEY_PREFIX = "nip05";
        private const string VERIFIED_CACHE_PREFIX = "nip05_verified";
        
        // Cache expiration times
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(1);
        private static readonly TimeSpan FAILED_CACHE_DURATION = TimeSpan.FromMinutes(15);

        public Nip05VerificationService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<Nip05VerificationService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            
            // Configure HttpClient for NIP-05 requests
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Netstr/2.0 (NIP-05)");
        }

        public async Task<Nip05Result> VerifyIdentifierAsync(string identifier, string pubkey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(pubkey))
                {
                    return Nip05Result.Invalid("Invalid identifier or pubkey");
                }

                // Parse identifier (user@domain.com or _@domain.com)
                var parts = identifier.Split('@');
                if (parts.Length != 2)
                {
                    return Nip05Result.Invalid("Invalid identifier format - must be user@domain");
                }

                var (user, domain) = (parts[0], parts[1]);
                
                // Validate domain format
                if (string.IsNullOrWhiteSpace(domain) || domain.Contains(' '))
                {
                    return Nip05Result.Invalid("Invalid domain format");
                }

                // Check cache first
                var cacheKey = $"{CACHE_KEY_PREFIX}:{domain}:{user}";
                if (_cache.TryGetValue(cacheKey, out Nip05CacheEntry? cached) && cached?.Response != null)
                {
                    _logger.LogDebug($"NIP-05 cache hit for {identifier}");
                    return ValidateResponse(cached.Response, user, pubkey);
                }

                // Fetch .well-known/nostr.json
                var url = $"https://{domain}/.well-known/nostr.json?name={user}";
                _logger.LogDebug($"Fetching NIP-05 verification from {url}");

                try
                {
                    var response = await _httpClient.GetStringAsync(url);
                    var nostrJson = JsonSerializer.Deserialize<Nip05Response>(response);

                    if (nostrJson == null)
                    {
                        var result = Nip05Result.Invalid("Invalid response format");
                        CacheFailedResult(cacheKey);
                        return result;
                    }

                    // Cache successful response
                    var cacheEntry = new Nip05CacheEntry { Response = nostrJson, FetchedAt = DateTime.UtcNow };
                    _cache.Set(cacheKey, cacheEntry, CACHE_DURATION);

                    var validationResult = ValidateResponse(nostrJson, user, pubkey);
                    
                    // Cache verified status if successful
                    if (validationResult.IsValid)
                    {
                        var verifiedCacheKey = $"{VERIFIED_CACHE_PREFIX}:{pubkey}";
                        _cache.Set(verifiedCacheKey, identifier, CACHE_DURATION);
                    }

                    return validationResult;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning($"HTTP error fetching NIP-05 for {identifier}: {ex.Message}");
                    var result = Nip05Result.Invalid($"Failed to fetch verification: {ex.Message}");
                    CacheFailedResult(cacheKey);
                    return result;
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning($"Timeout fetching NIP-05 for {identifier}: {ex.Message}");
                    var result = Nip05Result.Invalid("Request timeout");
                    CacheFailedResult(cacheKey);
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning($"JSON parsing error for NIP-05 {identifier}: {ex.Message}");
                    var result = Nip05Result.Invalid("Invalid JSON response");
                    CacheFailedResult(cacheKey);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error verifying NIP-05 for {identifier}");
                return Nip05Result.Invalid($"Verification failed: {ex.Message}");
            }
        }

        public Task<string?> GetVerifiedIdentifierAsync(string pubkey)
        {
            if (string.IsNullOrWhiteSpace(pubkey))
                return Task.FromResult<string?>(null);

            var cacheKey = $"{VERIFIED_CACHE_PREFIX}:{pubkey}";
            if (_cache.TryGetValue(cacheKey, out string? cachedIdentifier))
            {
                return Task.FromResult<string?>(cachedIdentifier);
            }

            return Task.FromResult<string?>(null);
        }

        public async Task<bool> IsIdentifierVerifiedAsync(string identifier, string pubkey)
        {
            var result = await VerifyIdentifierAsync(identifier, pubkey);
            return result.IsValid;
        }

        private Nip05Result ValidateResponse(Nip05Response response, string user, string pubkey)
        {
            if (response?.Names == null)
            {
                return Nip05Result.Invalid("No names found in response");
            }

            if (response.Names.TryGetValue(user, out var storedPubkey))
            {
                if (string.Equals(storedPubkey, pubkey, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"NIP-05 verification successful for {user} -> {pubkey}");
                    return Nip05Result.Valid();
                }
                else
                {
                    _logger.LogWarning($"NIP-05 pubkey mismatch for {user}: expected {pubkey}, got {storedPubkey}");
                    return Nip05Result.Invalid("Public key mismatch");
                }
            }

            _logger.LogWarning($"NIP-05 name {user} not found in response");
            return Nip05Result.Invalid("Name not found in verification response");
        }

        private void CacheFailedResult(string cacheKey)
        {
            // Cache failed results for shorter duration to prevent repeated failed requests
            var failedEntry = new Nip05CacheEntry 
            { 
                Response = null, 
                FetchedAt = DateTime.UtcNow 
            };
            _cache.Set(cacheKey, failedEntry, FAILED_CACHE_DURATION);
        }

        private class Nip05CacheEntry
        {
            public Nip05Response? Response { get; set; }
            public DateTime FetchedAt { get; set; }
        }
    }
}