# Priority NIPs Implementation Guide

This document provides detailed, step-by-step implementation guides for the high-impact NIPs that would significantly improve Netstr's client compatibility and ecosystem integration.

## Priority 1: High-Impact NIPs

### NIP-50: Search Capability

**Status**: Expected by most clients | **Impact**: High | **Difficulty**: Medium

#### Implementation Overview
NIP-50 adds a `search` field to REQ messages, enabling full-text search across event content.

#### Step-by-Step Implementation

**1. Extend SubscriptionFilter Model**
```csharp
// In src/Netstr/Messaging/Models/SubscriptionFilter.cs
public class SubscriptionFilter
{
    // Existing properties...
    
    [JsonPropertyName("search")]
    public string? Search { get; set; }
}
```

**2. Update Filter Parsing**
```csharp
// In src/Netstr/Messaging/MessageHandlers/SubscribeMessageHandler.cs
private SubscriptionFilter ParseFilter(JsonElement filterElement)
{
    var filter = new SubscriptionFilter();
    
    // Existing parsing...
    
    if (filterElement.TryGetProperty("search", out var searchElement))
    {
        filter.Search = searchElement.GetString();
    }
    
    return filter;
}
```

**3. Implement Search Matcher**
```csharp
// Create new file: src/Netstr/Messaging/Subscriptions/SearchMatcher.cs
public static class SearchMatcher
{
    public static bool MatchesSearch(Event eventItem, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return true;
            
        var content = eventItem.Content?.ToLowerInvariant() ?? "";
        var terms = searchTerm.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Basic implementation: all terms must be present
        return terms.All(term => content.Contains(term));
    }
    
    // Advanced: Support search extensions
    public static bool MatchesAdvancedSearch(Event eventItem, string searchTerm)
    {
        // Parse extensions like "include:spam", "domain:example.com"
        var (cleanTerm, extensions) = ParseSearchExtensions(searchTerm);
        
        if (!MatchesSearch(eventItem, cleanTerm))
            return false;
            
        // Apply extensions
        foreach (var ext in extensions)
        {
            if (!ApplySearchExtension(eventItem, ext))
                return false;
        }
        
        return true;
    }
}
```

**4. Update Database Query for Performance**
```csharp
// In src/Netstr/Messaging/Events/DbExtensions.cs
public static IQueryable<EventEntity> WhereMatchesSearch(
    this IQueryable<EventEntity> query, 
    string searchTerm)
{
    if (string.IsNullOrEmpty(searchTerm))
        return query;
        
    // Use PostgreSQL full-text search for performance
    return query.Where(e => EF.Functions.ToTsVector("english", e.Content)
        .Matches(EF.Functions.ToTsQuery("english", searchTerm)));
}
```

**5. Add PostgreSQL Full-Text Search Index**
```csharp
// Create migration: Add_Search_Index
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        "CREATE INDEX IF NOT EXISTS ix_events_content_fts ON events " +
        "USING gin(to_tsvector('english', content))");
}
```

**6. Update Subscription Matching**
```csharp
// In src/Netstr/Messaging/Subscriptions/SubscriptionFilterMatcher.cs
public static bool EventMatchesFilter(Event eventItem, SubscriptionFilter filter)
{
    // Existing checks...
    
    if (!string.IsNullOrEmpty(filter.Search))
    {
        if (!SearchMatcher.MatchesAdvancedSearch(eventItem, filter.Search))
            return false;
    }
    
    return true;
}
```

**7. Configuration and Limits**
```csharp
// In src/Netstr/Options/LimitsOptions.cs
public class SearchLimits
{
    public int MaxSearchTermLength { get; set; } = 100;
    public int MaxSearchResults { get; set; } = 1000;
    public bool EnableAdvancedSearch { get; set; } = true;
}
```

---

### NIP-96: HTTP File Storage

**Status**: Essential for media clients | **Impact**: Very High | **Difficulty**: High

#### Implementation Overview
Provides REST API for file uploads/downloads with Nostr authentication integration.

#### Step-by-Step Implementation

**1. Create File Storage Models**
```csharp
// Create new file: src/Netstr/Models/FileStorage/UploadRequest.cs
public class FileUploadRequest
{
    public IFormFile File { get; set; }
    public string? Caption { get; set; }
    public long? Expiration { get; set; }
    public string? MediaType { get; set; }
    public string? Alt { get; set; }
}

public class FileMetadata
{
    public string Hash { get; set; }
    public string Url { get; set; }
    public string MimeType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

**2. Create File Storage Service**
```csharp
// Create new file: src/Netstr/Services/FileStorageService.cs
public interface IFileStorageService
{
    Task<FileMetadata> StoreFileAsync(IFormFile file, string userPubkey, FileUploadRequest request);
    Task<Stream?> GetFileAsync(string hash);
    Task<FileMetadata?> GetFileMetadataAsync(string hash);
    Task<bool> DeleteFileAsync(string hash, string userPubkey);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<FileStorageService> _logger;
    
    public async Task<FileMetadata> StoreFileAsync(IFormFile file, string userPubkey, FileUploadRequest request)
    {
        // 1. Calculate SHA-256 hash
        var hash = await CalculateFileHashAsync(file);
        
        // 2. Check if file already exists
        if (await FileExistsAsync(hash))
            return await GetFileMetadataAsync(hash);
            
        // 3. Store file
        var filePath = Path.Combine(_storageRoot, hash);
        using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);
        
        // 4. Store metadata in database
        var metadata = new FileMetadata
        {
            Hash = hash,
            Url = $"/files/{hash}",
            MimeType = file.ContentType,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = userPubkey,
            ExpiresAt = request.Expiration.HasValue ? 
                DateTimeOffset.FromUnixTimeSeconds(request.Expiration.Value).DateTime : null
        };
        
        await StoreMetadataAsync(metadata);
        return metadata;
    }
}
```

**3. Add Database Entities**
```csharp
// In src/Netstr/Data/FileEntity.cs
public class FileEntity
{
    public string Hash { get; set; } // Primary key
    public string MimeType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Caption { get; set; }
    public string? Alt { get; set; }
}
```

**4. Create File Storage Controller**
```csharp
// Create new file: src/Netstr/Controllers/FileStorageController.cs
[ApiController]
public class FileStorageController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly INip98AuthService _auth;
    
    [HttpGet("/.well-known/nostr/nip96.json")]
    public IActionResult GetServerInfo()
    {
        return Ok(new
        {
            api_url = $"{Request.Scheme}://{Request.Host}/api/v1/upload",
            download_url = $"{Request.Scheme}://{Request.Host}/files",
            supported_nips = new[] { 96, 98 },
            tos_url = "https://yoursite.com/tos",
            content_types = new[] { "image/*", "video/*", "audio/*" },
            plans = new
            {
                free = new
                {
                    name = "Free",
                    max_byte_size = 10_000_000, // 10MB
                    file_expiry = new[] { 86400, 604800 }, // 1 day, 1 week
                    media_transformations = new
                    {
                        image = new[] { "resizing" }
                    }
                }
            }
        });
    }
    
    [HttpPost("/api/v1/upload")]
    public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request)
    {
        // 1. Validate NIP-98 authorization
        var authResult = await _auth.ValidateAuthorizationAsync(Request);
        if (!authResult.IsValid)
            return Unauthorized(new { status = "error", message = "auth-required" });
            
        // 2. Validate file
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { status = "error", message = "No file provided" });
            
        if (request.File.Length > 10_000_000) // 10MB limit
            return BadRequest(new { status = "error", message = "File too large" });
            
        // 3. Store file
        try
        {
            var metadata = await _fileStorage.StoreFileAsync(request.File, authResult.Pubkey, request);
            
            return Ok(new
            {
                status = "success",
                message = "Upload successful",
                nip94_event = new
                {
                    tags = new[]
                    {
                        new[] { "url", metadata.Url },
                        new[] { "x", metadata.Hash },
                        new[] { "size", metadata.Size.ToString() },
                        new[] { "m", metadata.MimeType }
                    }
                },
                url = metadata.Url
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = "Upload failed" });
        }
    }
    
    [HttpGet("/files/{hash}")]
    public async Task<IActionResult> DownloadFile(string hash)
    {
        var stream = await _fileStorage.GetFileAsync(hash);
        if (stream == null)
            return NotFound();
            
        var metadata = await _fileStorage.GetFileMetadataAsync(hash);
        return File(stream, metadata.MimeType);
    }
}
```

**5. Implement NIP-98 Authorization Service**
```csharp
// Create new file: src/Netstr/Services/Nip98AuthService.cs
public interface INip98AuthService
{
    Task<AuthResult> ValidateAuthorizationAsync(HttpRequest request);
}

public class Nip98AuthService : INip98AuthService
{
    public async Task<AuthResult> ValidateAuthorizationAsync(HttpRequest request)
    {
        // 1. Get Authorization header
        if (!request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthResult.Fail("Missing authorization header");
            
        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Nostr "))
            return AuthResult.Fail("Invalid authorization format");
            
        // 2. Decode base64 event
        var base64Event = headerValue.Substring(6);
        var eventJson = Encoding.UTF8.GetString(Convert.FromBase64String(base64Event));
        var authEvent = JsonSerializer.Deserialize<Event>(eventJson);
        
        // 3. Validate auth event
        if (authEvent.Kind != 27235)
            return AuthResult.Fail("Invalid auth event kind");
            
        // 4. Validate signature
        if (!await ValidateEventSignature(authEvent))
            return AuthResult.Fail("Invalid signature");
            
        // 5. Check timestamp (within 60 seconds)
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - authEvent.CreatedAt) > 60)
            return AuthResult.Fail("Auth event too old");
            
        // 6. Validate URL and method tags
        var urlTag = authEvent.Tags.FirstOrDefault(t => t.Name == "u");
        var methodTag = authEvent.Tags.FirstOrDefault(t => t.Name == "method");
        
        if (urlTag?.Value != GetFullUrl(request))
            return AuthResult.Fail("URL mismatch");
            
        if (methodTag?.Value != request.Method)
            return AuthResult.Fail("Method mismatch");
            
        return AuthResult.Success(authEvent.Pubkey);
    }
}
```

---

### NIP-05: DNS-based Identities

**Status**: Widely used for verification | **Impact**: High | **Difficulty**: Low

#### Step-by-Step Implementation

**1. Create NIP-05 Verification Service**
```csharp
// Create new file: src/Netstr/Services/Nip05VerificationService.cs
public interface INip05VerificationService
{
    Task<Nip05Result> VerifyIdentifierAsync(string identifier, string pubkey);
    Task<string?> GetVerifiedIdentifierAsync(string pubkey);
}

public class Nip05VerificationService : INip05VerificationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    
    public async Task<Nip05Result> VerifyIdentifierAsync(string identifier, string pubkey)
    {
        try
        {
            // 1. Parse identifier (user@domain.com or _@domain.com)
            var parts = identifier.Split('@');
            if (parts.Length != 2)
                return Nip05Result.Invalid("Invalid identifier format");
                
            var (user, domain) = (parts[0], parts[1]);
            
            // 2. Fetch .well-known/nostr.json
            var url = $"https://{domain}/.well-known/nostr.json?name={user}";
            var cacheKey = $"nip05:{domain}:{user}";
            
            if (_cache.TryGetValue(cacheKey, out Nip05Response? cached))
            {
                return ValidateResponse(cached, user, pubkey);
            }
            
            var response = await _httpClient.GetStringAsync(url);
            var nostrJson = JsonSerializer.Deserialize<Nip05Response>(response);
            
            // 3. Cache for 1 hour
            _cache.Set(cacheKey, nostrJson, TimeSpan.FromHours(1));
            
            return ValidateResponse(nostrJson, user, pubkey);
        }
        catch (Exception ex)
        {
            return Nip05Result.Invalid($"Verification failed: {ex.Message}");
        }
    }
    
    private Nip05Result ValidateResponse(Nip05Response response, string user, string pubkey)
    {
        if (response?.Names?.TryGetValue(user, out var storedPubkey) == true)
        {
            if (storedPubkey == pubkey)
                return Nip05Result.Valid();
            else
                return Nip05Result.Invalid("Pubkey mismatch");
        }
        
        return Nip05Result.Invalid("Name not found");
    }
}

public class Nip05Response
{
    [JsonPropertyName("names")]
    public Dictionary<string, string>? Names { get; set; }
    
    [JsonPropertyName("relays")]
    public Dictionary<string, string[]>? Relays { get; set; }
}
```

**2. Add NIP-05 Validation to Event Processing**
```csharp
// Create new file: src/Netstr/Messaging/Events/Validators/Nip05Validator.cs
public class Nip05Validator : IEventValidator
{
    private readonly INip05VerificationService _nip05Service;
    
    public async Task<ValidationResult> ValidateEventAsync(Event e, ClientContext context)
    {
        // Only validate kind 0 (metadata) events
        if (e.Kind != 0)
            return ValidationResult.Success();
            
        try
        {
            var content = JsonSerializer.Deserialize<UserMetadata>(e.Content);
            if (!string.IsNullOrEmpty(content?.Nip05))
            {
                var result = await _nip05Service.VerifyIdentifierAsync(content.Nip05, e.Pubkey);
                if (!result.IsValid)
                {
                    // Don't reject, just log for monitoring
                    _logger.LogWarning($"NIP-05 verification failed for {e.Pubkey}: {result.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"NIP-05 validation error for event {e.Id}");
        }
        
        return ValidationResult.Success();
    }
}

public class UserMetadata
{
    [JsonPropertyName("nip05")]
    public string? Nip05 { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    // Other metadata fields...
}
```

---

## Priority 2: Ecosystem Integration NIPs

### NIP-98: HTTP Authorization

**Required for NIP-96 file uploads**

Implementation details included in NIP-96 section above (`Nip98AuthService`).

### NIP-78: Application-specific Data

**Status**: Better client experience | **Impact**: Medium | **Difficulty**: Low

#### Implementation
Uses addressable events (kind 30078) - leverage existing `AddressableEventHandler`:

```csharp
// Add to EventKind enum
ApplicationSpecificData = 30078,

// No additional handler needed - AddressableEventHandler handles it
// Events use 'd' tag with app identifier
// Content can be encrypted for private app data
```

## Implementation Priority Order

1. **NIP-05** (Low effort, high adoption impact)
2. **NIP-50** (Medium effort, widely expected feature)  
3. **NIP-98** (Required for file storage)
4. **NIP-96** (High effort, high value for media clients)
5. **NIP-78** (Low effort, nice-to-have)

Each implementation can be done independently, leveraging Netstr's excellent architectural foundation.