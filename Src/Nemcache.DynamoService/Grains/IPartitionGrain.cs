using System.Threading.Tasks;
using Orleans;

namespace Nemcache.DynamoService.Grains
{
    public interface IPartitionGrain : IGrainWithStringKey
    {
        Task PutAsync(string key, byte[] value);
        Task PutReplicaAsync(string key, byte[] value);
        Task<byte[]?> GetAsync(string key);
        Task<byte[]?> GetReplicaAsync(string key);
    }
}
