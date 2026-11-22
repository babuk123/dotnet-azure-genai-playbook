namespace Interview;

// #20 Producer/consumer with Channel<T> (process a big message backlog)  [Advanced]
// Problem : Messages arrive faster than you can process them. Build a bounded queue with several consumers that process in parallel and apply back-pressure.
// Approach: System.Threading.Channels: a bounded channel blocks producers when full (back-pressure); N consumer tasks read with ReadAllAsync; complete the writer to finish.
// Complexity: Throughput scales with consumers until the downstream limit.

public static class Pipeline
{
    public static async Task<int> ProcessAsync(IEnumerable<int> messages, int consumers = 4)
    {
        var channel = System.Threading.Channels.Channel.CreateBounded<int>(
            new System.Threading.Channels.BoundedChannelOptions(100)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
            });
        int processed = 0;

        var workers = Enumerable.Range(0, consumers).Select(_ => Task.Run(async () =>
        {
            await foreach (var msg in channel.Reader.ReadAllAsync())
            {
                await Task.Yield();                // process the message (I/O)
                Interlocked.Increment(ref processed);
            }
        })).ToArray();

        foreach (var m in messages)
            await channel.Writer.WriteAsync(m);   // waits when the queue is full
        channel.Writer.Complete();

        await Task.WhenAll(workers);
        return processed;
    }
}
