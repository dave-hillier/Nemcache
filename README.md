# Nemcache

Nemcache is a persistent, key-value store implemented in C# targeting .NET 8.
It is client compatible with [Memcache](http://memcached.org/) (an in-memory key-value store).

## Installation

Nemcache uses [TopShelf](http://topshelf-project.com/) to handle service installation. 
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

To install as a windows service:
```
dotnet run --project Src/Nemcache.Service -- install
```
Followed by this to start:
```
dotnet run --project Src/Nemcache.Service -- start
```
For more information about the command line parameters use help:
```
dotnet run --project Src/Nemcache.Service -- help
```

## Status

The following commands from the [Memcached specification](https://raw.github.com/memcached/memcached/master/doc/protocol.txt) 
are implemented: get (and gets), set, add, replace, delete, append, prepend, incr, decr, touch, flush_all, cas, quit and version.
 
Currently only TCP is supported including the noreply mode. 

Flags support up-to 64 bit values, although this is a divergence from the original spec and therefore might not be supported by your client.

The cache is persisted to file so state can be maintained across cache restarts.

## React client

The `client` folder contains a small React application that replicates the
behaviour of `test.html`. It opens a WebSocket connection and sends `PUT`
requests when the button is pressed. To run the client in development mode:

```bash
cd client
yarn
yarn dev
```

To create a production build run `yarn build`.

