using System.Collections.Generic;

namespace Nemcache.DynamoService.Routing;

public interface IRing
{
    IEnumerable<string> GetNodes(string key, int count);
}
