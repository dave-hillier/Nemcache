# Nemcache

Nemcache is a persistent, key-value store implemented in C# and now targets .NET 6. It is client compatible with [Memcache](http://memcached.org/) (an in-memory key-value store).

## Installation

First, clone the repository:
```
git clone https://github.com/dave-hillier/nemcache.git
```

After cloning the repository, run `dotnet build` inside the `Src` directory.

To start the service as a command line application, execute:
```
dotnet run --project Src/Nemcache.Service
```

## Continuous Integration

This repository uses GitHub Actions to build and test the solution on each push
and pull request. The workflow definition can be found in
[.github/workflows/dotnet.yml](.github/workflows/dotnet.yml).

## Status

The following commands from the [Memcached specification](https://raw.github.com/memcached/memcached/master/doc/protocol.txt) 
are implemented: get (and gets), set, add, replace, delete, append, prepend, incr, decr, touch, flush_all, cas, quit and version.
 
Currently only TCP is supported including the noreply mode. 

Flags support up-to 64 bit values, although this is a divergence from the original spec and therefore might not be supported by your client.

The cache is persisted to file so state can be maintained across cache restarts. 

