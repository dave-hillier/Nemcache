nemcached
=========

Nemcached is a Memcached server implemented in C#. [Memcached](http://memcached.org/) is an in-memory key-value store for small chunks of arbitrary data (strings, objects) from results of database calls, API calls, or page rendering.

Status
======

Nemcached is not yet ready for production use due to lack of thread safety or performance testing (and probably optimisation)

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

Eviction works by evicting a random cache entry until the cache can insert the new value.

