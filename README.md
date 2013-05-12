# nemcached

Nemcached is a Memcached server implemented in C# 4.5. 
[Memcached](http://memcached.org/) is an in-memory key-value store for small chunks of arbitrary data (strings, objects) from results of database calls, API calls, or page rendering.
Nemcached can be used as a drop in replacement and is compatible with existing Memcache clients.

## Installation

Nemcached uses [TopShelf](http://topshelf-project.com/) to handle service installation. 
Clone the repository:
```
git clone https://github.com/dave-hillier/nemcached.git
```

After cloning the repository, run either `msbuild` or open `Nemcached.Sln` in Visual Studio and build. 

To start the service as a command line application, simply run `Nemcache.Service.exe` 

To install as a windows service:
```
Nemcache.Service.exe install
```
Followed by this to start:
```
Nemcache.Service.exe start
```
For more information about the command line parameters use help:
```
Nemcache.Service.exe help
```

## Status

The following commands from the [Memcached specification](https://raw.github.com/memcached/memcached/master/doc/protocol.txt) 
are implemented: get (and gets), set, add, replace, delete, append, prepend, incr, decr, touch, flush_all, cas, quit and version.
 
Currently only TCP is supported including the noreply mode. 
Flags support up-to 64 bit values, although this is a divergence from the original spec and therefore might not be supported by your client.
Eviction works by evicting a random cache entry until the cache can insert the new value. 
The cache is persisted to files so state can be maintained across cache restarts.

