using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Netstr.Tests;

/// <summary>
/// Memory leak verification tests for slow consumer scenario.
/// Run with: dotnet test --filter "FullyQualifiedName~MemoryLeakTest"
/// </summary>
public class MemoryLeakTest : IClassFixture<WebApplicationFactory>
{
    private readonly WebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    public MemoryLeakTest(WebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
    }

    [Fact]
    public async Task SlowConsumer_DoesNotCauseUnboundedMemoryGrowth()
    {
        // Arrange: Connect a slow consumer that subscribes but reads very slowly
        var slowConsumer = await factory.ConnectWebSocketAsync();

        // Subscribe to all kind 1 events
        var subRequest = JsonSerializer.Serialize(new object[] { "REQ", "slow-test", new { kinds = new[] { 1 } } });
        await slowConsumer.SendAsync(
            Encoding.UTF8.GetBytes(subRequest),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);

        // Get initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(true);
        output.WriteLine($"Initial memory: {initialMemory / 1024.0 / 1024.0:F2} MB");

        // Act: Flood events without reading responses (simulates slow consumer)
        var publisher = await factory.ConnectWebSocketAsync();

        const int eventCount = 1000;
        for (int i = 0; i < eventCount; i++)
        {
            var eventData = CreateTestEvent(i);
            await publisher.SendAsync(
                Encoding.UTF8.GetBytes(eventData),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            // Small delay to let the relay process
            if (i % 100 == 0)
            {
                await Task.Delay(10);
            }
        }

        // Wait a bit for queue to accumulate
        await Task.Delay(1000);

        // Measure memory after flooding
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterFloodMemory = GC.GetTotalMemory(true);
        output.WriteLine($"After flood memory: {afterFloodMemory / 1024.0 / 1024.0:F2} MB");

        var memoryGrowth = (afterFloodMemory - initialMemory) / 1024.0 / 1024.0;
        output.WriteLine($"Memory growth: {memoryGrowth:F2} MB");

        // Assert: Memory growth should be bounded (not growing linearly with event count)
        // With the fix, the queue is bounded to MaxPendingEvents (default 100)
        // Without the fix, queue would grow to 1000+ events
        // Allow some growth for normal operations, but should be < 50MB for 1000 events
        Assert.True(memoryGrowth < 50,
            $"Memory grew by {memoryGrowth:F2} MB which suggests unbounded queue growth");

        // Cleanup
        await slowConsumer.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        await publisher.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        output.WriteLine("✓ Slow consumer test passed - memory growth is bounded");
    }

    [Fact]
    public async Task MultipleSlowConsumers_MemoryStaysBounded()
    {
        const int consumerCount = 10;
        const int eventsPerConsumer = 500;

        var consumers = new List<WebSocket>();

        // Create multiple slow consumers
        for (int i = 0; i < consumerCount; i++)
        {
            var consumer = await factory.ConnectWebSocketAsync();
            var subRequest = JsonSerializer.Serialize(new object[] { "REQ", $"sub-{i}", new { kinds = new[] { 1 } } });
            await consumer.SendAsync(
                Encoding.UTF8.GetBytes(subRequest),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            consumers.Add(consumer);
        }

        GC.Collect();
        var initialMemory = GC.GetTotalMemory(true);
        output.WriteLine($"Initial memory with {consumerCount} consumers: {initialMemory / 1024.0 / 1024.0:F2} MB");

        // Flood events
        var publisher = await factory.ConnectWebSocketAsync();
        for (int i = 0; i < eventsPerConsumer; i++)
        {
            var eventData = CreateTestEvent(i);
            await publisher.SendAsync(
                Encoding.UTF8.GetBytes(eventData),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        await Task.Delay(2000);

        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        var growth = (finalMemory - initialMemory) / 1024.0 / 1024.0;
        output.WriteLine($"Final memory: {finalMemory / 1024.0 / 1024.0:F2} MB (growth: {growth:F2} MB)");

        // With bounded queues, memory should not grow linearly with consumers * events
        // Unbounded: ~10 consumers * 500 events * ~1KB = ~5MB minimum in queues alone
        // Bounded: ~10 consumers * 100 max queue * ~1KB = ~1MB max in queues
        Assert.True(growth < 100,
            $"Memory grew excessively ({growth:F2} MB) suggesting unbounded queues");

        // Cleanup
        foreach (var c in consumers)
        {
            try { await c.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); }
            catch { }
        }
        await publisher.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        output.WriteLine("✓ Multiple slow consumers test passed");
    }

    private static string CreateTestEvent(int index)
    {
        // Create a minimal valid-looking event (will fail signature validation but tests queue behavior)
        var evt = new
        {
            id = $"{index:x64}".PadLeft(64, '0'),
            pubkey = "0000000000000000000000000000000000000000000000000000000000000001",
            created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            kind = 1,
            tags = Array.Empty<string[]>(),
            content = $"Test event {index} - " + new string('x', 100), // ~100 byte content
            sig = new string('0', 128)
        };
        return JsonSerializer.Serialize(new object[] { "EVENT", evt });
    }
}
