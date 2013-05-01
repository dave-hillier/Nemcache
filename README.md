nemcached
=========

Nemcached is a Memcached server implemented in C#. 
[Memcached](http://memcached.org/) is an in-memory key-value store for small chunks of arbitrary data (strings, objects) from results of database calls, API calls, or page rendering.
Nemcached can be used as a drop in replacement and is compatible with existing Memcache clients.

Status
======

Nemcached is not yet ready for production use as it, although functionally complete enough to be used, 
not fully tested and is unoptimised.

The following commands from the [Memcached specification](https://raw.github.com/memcached/memcached/master/doc/protocol.txt) are implemented:
* get (and gets)
* set 
* add
* replace
* delete
* append
* prepend
* incr
* decr
* touch
* flush_all
* cas
* quit
* version

Currently only TCP is supported including the noreply mode. 
Flags support up-to 64 bit values, although this is a divergence from the original spec and therefore might not be supported by your client.
Eviction works by evicting a random cache entry until the cache can insert the new value. 
The cache is persisted to files so state can be maintained across cache restarts.



An aim is to keep compatible with Memcached as a drop in replacement and also provide and embeddedable key value store for c#.


To-do
=====
* Compacting to cache log.
* Persistence for Cas and mutate commands
* Performance testing and optimisation
* Client API
* Configuration

Future
======
* Proxy client to locally cache specific keys.
* client permissioning and authentication.
* Control the mechanism for each key? Maybe allow some keys to be transisent, persistent etc.


