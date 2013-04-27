using ProtoBuf;

namespace Nemcache.Service.Notifications
{
    internal interface ICacheNotification
    {
        int SequenceId { get; }
    }
}