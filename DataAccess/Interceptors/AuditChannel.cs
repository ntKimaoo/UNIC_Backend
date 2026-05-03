using System.Threading.Channels;

namespace DataAccess.Interceptors
{
    /// <summary>
    /// Singleton in-memory channel between AuditInterceptor (producer)
    /// and RecordOfChangeProcessorService (consumer).
    /// </summary>
    public sealed class AuditChannel
    {
        private readonly Channel<PendingAuditEntry> _channel =
            Channel.CreateUnbounded<PendingAuditEntry>(
                new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false });

        public ChannelWriter<PendingAuditEntry> Writer => _channel.Writer;
        public ChannelReader<PendingAuditEntry> Reader => _channel.Reader;
    }
}
