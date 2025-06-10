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
are implemented: get (and gets), set, add, replace, delete, append, prepend, incr, decr, touch, flush_all, cas, quit, stats (including `stats settings`) and version.
The `stats` command reports values such as the server's `pid`, `uptime`, `version`, `pointer_size`, current item count and memory usage.
 
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


## Bitcask-style persistence

For environments with large data sets, Nemcache can switch to a Bitcask-inspired store.
Set `NEMCACHE_USE_BITCASK=1` before starting the service to enable this mode.
A design overview is available in [docs/bitcask-notes.md](docs/bitcask-notes.md).

The default persistence records each cache operation to a log and replays the log
on startup. In Bitcask mode, updates are written as raw key/value pairs to
numbered data files while an in-memory directory tracks the latest value offset
for each key. Compaction merges the most recent entries into new files.

### Trade-offs

The existing persistence layer logs cache notifications to a stream. Recovery
replays this log in order to rebuild the cache. The Bitcask approach instead
writes raw key/value pairs and keeps an in-memory index of offsets. Each
strategy has strengths:

* **Write pattern and disk layout** – Both append to a log, but Bitcask stores
  the actual data with offsets while the stream stores serialized notifications.
* **Read and recovery performance** – The Bitcask index allows direct lookup and
  faster restarts. Stream-based recovery must replay the entire log.
* **Memory usage** – Bitcask uses memory proportional to the number of keys for
  its index. The original stream approach avoids that overhead.
* **Storage overhead** – StreamArchiver keeps every notification until
  compaction, whereas Bitcask compaction keeps only the latest values, typically
  using less space.
* **Complexity** – The stream approach is simple to implement. Bitcask adds file
  management and indexing logic in exchange for improved performance on large
  data sets.
