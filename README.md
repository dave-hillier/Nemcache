nemcached
=========

Nemcached is a Memcached server implemented in C#. 
[Memcached](http://memcached.org/) is an in-memory key-value store for small chunks of arbitrary data (strings, objects) from results of database calls, API calls, or page rendering.
Nemcached can be used as a drop in replacement and is compatible with existing Memcache clients.

Status
======

Nemcached is not yet ready for production use as it does not support concurrent access and, 
although functionally complete enough to be used, it is unoptimised.

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
* flush_all (without delay)

Currently only TCP is supported including the noreply mode. 
Flags support up-to 64 bit values, although this is a divergence from the original spec and therefore might not be supported by your client.
Eviction works by evicting a random cache entry until the cache can insert the new value.

TODO List
=========
*Run as service support
*Configuration
*Add support for the remaining commands:
  * CAS, the cas unique field is not currently supported
  * flush_all (with a delay)
  * quit
  * Stats
  * version
*Fix concurrent access
*UDP Protocol

Future work
===========
*Implement a client. The test classes
*File-backed Persistent cache; to mitigate cold cache problems. 
Aim is to keep compatible with Memcached as a drop in replacement. 
