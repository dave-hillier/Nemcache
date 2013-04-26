namespace Nemcache.Service.Notifications
{
    internal class Touch : IKeyCacheNotification
    {
        public string Key { get; set; }
        public int SequenceId { get; set; }
    }
}