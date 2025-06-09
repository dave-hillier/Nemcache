using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Nemcache.DynamoService.Routing
{
    /// <summary>
    /// Simple consistent hash ring for mapping keys to nodes.
    /// </summary>
    public class ConsistentHashRing
    {
        private readonly SortedDictionary<uint, string> _ring = new();
        private readonly int _virtualNodes;

        public ConsistentHashRing(IEnumerable<string> nodes, int virtualNodes = 100)
        {
            _virtualNodes = virtualNodes;
            foreach (var n in nodes)
            {
                AddNode(n);
            }
        }

        private static uint Hash(string key)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToUInt32(bytes, 0);
        }

        public void AddNode(string node)
        {
            for (int i = 0; i < _virtualNodes; i++)
            {
                _ring[Hash($"{node}-{i}")] = node;
            }
        }

        public void RemoveNode(string node)
        {
            for (int i = 0; i < _virtualNodes; i++)
            {
                _ring.Remove(Hash($"{node}-{i}"));
            }
        }

        public string GetNode(string key)
        {
            if (_ring.Count == 0)
            {
                throw new InvalidOperationException("Hash ring is empty");
            }

            uint hash = Hash(key);
            foreach (var kv in _ring)
            {
                if (kv.Key >= hash)
                    return kv.Value;
            }
            return _ring[_ring.First().Key];
        }

        public IEnumerable<string> GetNodes(string key, int count)
        {
            if (_ring.Count == 0) throw new InvalidOperationException("Hash ring is empty");
            var keys = _ring.Keys.ToList();
            uint hash = Hash(key);
            int index = keys.BinarySearch(hash);
            if (index < 0) index = ~index;
            for (int i = 0; i < count; i++)
            {
                if (index >= keys.Count) index = 0;
                yield return _ring[keys[index]];
                index++;
            }
        }
    }
}
