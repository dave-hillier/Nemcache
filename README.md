# Nemcache

Nemcache is a persistent, key-value store implemented in C# targeting .NET 8.
It is client compatible with [Memcache](http://memcached.org/) (an in-memory key-value store).

## Installation

Nemcache now relies on the built in .NET generic host. The service can run
either as a console application or as a Windows service when installed via the
standard `sc` tooling.
First, clone the repository:
```
git clone https://github.com/dave-hillier/nemcache.git
```

After cloning the repository, restore packages and build using the .NET SDK:
```
dotnet build Src/Nemcache.Service/Nemcache.Service.csproj
```

To start the service as a command line application, simply run:
```
dotnet run --project Src/Nemcache.Service
```

To install as a Windows service publish the project and register it with `sc`:
```
dotnet publish -c Release -o out Src/Nemcache.Service/Nemcache.Service.csproj
sc create Nemcache binPath= "<path>\Nemcache.Service.exe"
```
After installation start the service with:
```
sc start Nemcache
```

## Status

The following commands from the [Memcached specification](https://raw.github.com/memcached/memcached/master/doc/protocol.txt) 
are implemented: get (and gets), set, add, replace, delete, append, prepend, incr, decr, touch, flush_all, cas, quit and version.
 
Currently only TCP is supported including the noreply mode. 

Flags support up-to 64 bit values, although this is a divergence from the original spec and therefore might not be supported by your client.

The cache is persisted to file so state can be maintained across cache restarts. 

