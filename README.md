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

An aim is to keep compatible with Memcached as a drop in replacement and also provide and embeddedable key value store for c#.


To-do
=====
* Add compacting to log file/cache persistence.
* Performance testing and optimisation

Future
======
* Clustering/distributed stuff
* Various other protocols



