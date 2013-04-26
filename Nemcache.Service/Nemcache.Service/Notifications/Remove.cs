namespace Nemcache.Service.Notifications
{
    internal class Remove : IKeyCacheNotification
    {
        public string Key { get; set; }
        public int SequenceId { get; set; }
    }
}