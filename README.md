nemcached
=========

Memcached server implemented in C#. Currently this only implements basic get and set functionality for small payloads. This is a work in progress and my intention is to implement the protocol as described [in this document](https://raw.github.com/memcached/memcached/master/doc/protocol.txt), or at least the TCP part.

Basic support for the following Memcached commands is implemented: get, set, append, prepend, incr and decr.

There is no cache eviction at the moment. Cached items will never expire or be evicted through the cache filling.
