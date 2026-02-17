using FluentAssertions;
using Netstr.Messaging.Models;
using Netstr.Tests.NIPs;
using System.Net.WebSockets;
using Xunit;
using Xunit.Abstractions;

namespace Netstr.Tests;

/// <summary>
/// Memory pressure tests for slow consumers.
/// Run with: dotnet test --filter "FullyQualifiedName~MemoryLeakTest"
/// </summary>
public class MemoryLeakTest : IClassFixture<WebApplicationFactory>
{
    private const double BytesPerMb = 1024d * 1024d;

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
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var slowConsumer = await this.factory.ConnectWebSocketAsync();
        using var publisher = await this.factory.ConnectWebSocketAsync();

        await slowConsumer.SendReqAsync(
            "slow-one",
            [new SubscriptionFilterRequest { Kinds = [1] }],
            timeout.Token);
        await WaitForEoseAsync(slowConsumer, "slow-one", timeout.Token);

        var initialMemory = ForceGcAndGetMemory();
        this.output.WriteLine($"Initial memory: {initialMemory / BytesPerMb:F2} MB");

        var baseTime = DateTimeOffset.UtcNow;
        await PublishAndAwaitOkAsync(publisher, baseTime, startIndex: 0, count: 600, timeout.Token);
        await Task.Delay(500, timeout.Token);

        var afterPhase1Memory = ForceGcAndGetMemory();
        this.output.WriteLine($"After phase 1 memory: {afterPhase1Memory / BytesPerMb:F2} MB");

        await PublishAndAwaitOkAsync(publisher, baseTime, startIndex: 600, count: 600, timeout.Token);
        await Task.Delay(500, timeout.Token);

        var afterPhase2Memory = ForceGcAndGetMemory();
        this.output.WriteLine($"After phase 2 memory: {afterPhase2Memory / BytesPerMb:F2} MB");

        var phase1GrowthMb = Math.Max(0, afterPhase1Memory - initialMemory) / BytesPerMb;
        var phase2GrowthMb = Math.Max(0, afterPhase2Memory - afterPhase1Memory) / BytesPerMb;
        var totalGrowthMb = Math.Max(0, afterPhase2Memory - initialMemory) / BytesPerMb;

        this.output.WriteLine($"Phase 1 growth: {phase1GrowthMb:F2} MB");
        this.output.WriteLine($"Phase 2 growth: {phase2GrowthMb:F2} MB");
        this.output.WriteLine($"Total growth: {totalGrowthMb:F2} MB");

        AssertBoundedGrowth(totalGrowthMb, phase1GrowthMb, phase2GrowthMb, "single slow consumer");
    }

    [Fact]
    public async Task MultipleSlowConsumers_MemoryStaysBounded()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var consumers = new List<WebSocket>();

        try
        {
            for (int i = 0; i < 5; i++)
            {
                var consumer = await this.factory.ConnectWebSocketAsync();
                await consumer.SendReqAsync(
                    $"slow-{i}",
                    [new SubscriptionFilterRequest { Kinds = [1] }],
                    timeout.Token);
                await WaitForEoseAsync(consumer, $"slow-{i}", timeout.Token);
                consumers.Add(consumer);
            }

            var initialMemory = ForceGcAndGetMemory();
            this.output.WriteLine($"Initial memory with {consumers.Count} slow consumers: {initialMemory / BytesPerMb:F2} MB");

            using var publisher = await this.factory.ConnectWebSocketAsync();
            var baseTime = DateTimeOffset.UtcNow.AddHours(1);

            await PublishAndAwaitOkAsync(publisher, baseTime, startIndex: 0, count: 500, timeout.Token);
            await Task.Delay(500, timeout.Token);
            var afterPhase1Memory = ForceGcAndGetMemory();

            await PublishAndAwaitOkAsync(publisher, baseTime, startIndex: 500, count: 500, timeout.Token);
            await Task.Delay(500, timeout.Token);
            var afterPhase2Memory = ForceGcAndGetMemory();

            var phase1GrowthMb = Math.Max(0, afterPhase1Memory - initialMemory) / BytesPerMb;
            var phase2GrowthMb = Math.Max(0, afterPhase2Memory - afterPhase1Memory) / BytesPerMb;
            var totalGrowthMb = Math.Max(0, afterPhase2Memory - initialMemory) / BytesPerMb;

            this.output.WriteLine($"Phase 1 growth: {phase1GrowthMb:F2} MB");
            this.output.WriteLine($"Phase 2 growth: {phase2GrowthMb:F2} MB");
            this.output.WriteLine($"Total growth: {totalGrowthMb:F2} MB");

            AssertBoundedGrowth(totalGrowthMb, phase1GrowthMb, phase2GrowthMb, "multiple slow consumers");
        }
        finally
        {
            foreach (var consumer in consumers)
            {
                try
                {
                    consumer.Abort();
                    consumer.Dispose();
                }
                catch
                {
                    // Best-effort cleanup for potentially blocked sockets.
                }
            }
        }
    }

    private static long ForceGcAndGetMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return GC.GetTotalMemory(true);
    }

    private static Event CreateValidEvent(int index, DateTimeOffset baseTime)
    {
        var e = new Event
        {
            Id = string.Empty,
            Signature = string.Empty,
            PublicKey = Alice.PublicKey,
            CreatedAt = baseTime.AddSeconds(index),
            Kind = 1,
            Tags = [],
            Content = $"memory-test-event-{index}-{new string('x', 128)}"
        };

        return Helpers.FinalizeEvent(e, Alice.PrivateKey);
    }

    private async Task PublishAndAwaitOkAsync(
        WebSocket publisher,
        DateTimeOffset baseTime,
        int startIndex,
        int count,
        CancellationToken token)
    {
        for (int i = 0; i < count; i++)
        {
            var e = CreateValidEvent(startIndex + i, baseTime);
            await publisher.SendEventAsync(e, token);
            await WaitForOkAsync(publisher, e.Id, token);
        }
    }

    private static async Task WaitForOkAsync(WebSocket ws, string eventId, CancellationToken token)
    {
        while (true)
        {
            var message = await ws.ReceiveOnceAsync(token);

            if (message.Length < 4)
            {
                continue;
            }

            if (message[0].GetString() != MessageType.Ok)
            {
                continue;
            }

            if (message[1].GetString() != eventId)
            {
                continue;
            }

            message[2].GetBoolean().Should().BeTrue($"event {eventId} should be accepted");
            return;
        }
    }

    private static async Task WaitForEoseAsync(WebSocket ws, string subscriptionId, CancellationToken token)
    {
        while (true)
        {
            var message = await ws.ReceiveOnceAsync(token);

            if (message.Length < 2)
            {
                continue;
            }

            if (message[0].GetString() == MessageType.EndOfStoredEvents &&
                message[1].GetString() == subscriptionId)
            {
                return;
            }
        }
    }

    private void AssertBoundedGrowth(double totalGrowthMb, double phase1GrowthMb, double phase2GrowthMb, string scenario)
    {
        totalGrowthMb.Should().BeLessThan(
            150,
            $"{scenario} should not show runaway memory growth");

        if (phase1GrowthMb > 1)
        {
            phase2GrowthMb.Should().BeLessThan(
                phase1GrowthMb * 0.9 + 12,
                $"{scenario} should show slower incremental growth in a second equal load phase");
        }
        else
        {
            phase2GrowthMb.Should().BeLessThan(
                20,
                $"{scenario} second-phase growth should still stay bounded when phase 1 growth is near zero");
        }
    }
}
