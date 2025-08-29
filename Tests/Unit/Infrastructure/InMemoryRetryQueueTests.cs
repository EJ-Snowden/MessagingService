using Infrastructure.Repositories;
using FluentAssertions;

namespace Tests.Unit.Infrastructure;

public sealed class InMemoryRetryQueueTests
{
    [Fact]
    public async Task Enqueue_and_dequeue_due_returns_only_due_items_and_removes_them()
    {
        var q = new InMemoryRetryQueue();
        var now = DateTime.UtcNow;

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        await q.EnqueueAsync(id1, now.AddSeconds(-10));
        await q.EnqueueAsync(id2, now.AddSeconds(10));
        await q.EnqueueAsync(id3, now.AddSeconds(-5));

        var due = await q.DequeueDueAsync(now, max: 10);

        due.Select(x => x.Id).Should().BeEquivalentTo([id1, id3]);
        (await q.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Dequeue_due_respects_max()
    {
        var q = new InMemoryRetryQueue();
        var now = DateTime.UtcNow;

        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var id in ids)
            await q.EnqueueAsync(id, now.AddSeconds(-1));

        var due = await q.DequeueDueAsync(now, max: 2);

        due.Should().HaveCount(2);
        (await q.CountAsync()).Should().Be(3);
    }
}