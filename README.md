# Nemcache

Nemcache is a persistent, key-value store implemented in C# 4.5. 
It is client compatible with [Memcache](http://memcached.org/) (an in-memory key-value store).

## Installation

Nemcache uses [TopShelf](http://topshelf-project.com/) to handle service installation. 
First, clone the repository:
```
git clone https://github.com/dave-hillier/nemcache.git
```

After cloning the repository, run either `msbuild` or open `Nemcache.Sln` in Visual Studio and build. 

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

The cache is persisted to files so state can be maintained across cache restarts.

