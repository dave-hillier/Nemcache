namespace Nemcache.DynamoService.Routing
{
    /// <summary>
    /// Options controlling the behaviour of <see cref="RingProvider"/>.
    /// </summary>
    public class RingProviderOptions
    {
        public int PartitionCount { get; set; }
        public int ReplicaCount { get; set; }
    }
}
