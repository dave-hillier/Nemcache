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


## Persistence strategies

Nemcache provides three persistence options for the on-disk cache log:

* **Stream log (default)** – Each cache notification is serialized using
  protobuf and appended to `cachelog.bin`. On startup the log is replayed to
  restore the cache state.
* **Bitcask store** – When `NEMCACHE_USE_BITCASK=1` is set, updates are written
  as raw key/value pairs to numbered data files while an in-memory directory
  tracks the latest offset for each key. A design overview is available in
  [docs/bitcask-notes.md](docs/bitcask-notes.md).
* **Hybrid log** – A memory buffer temporarily holds updates before flushing
  them to a single append-only file. An in-memory index maps keys to offsets in
  either the buffer or the persisted log, allowing direct lookups.

### Trade-offs

| Strategy | Write layout | Recovery speed | Memory usage | Disk usage | Complexity |
| -------- | ------------ | -------------- | ------------ | ---------- | ---------- |
| **Stream log** | Append protobuf notifications to a single log file | Must replay entire log on startup | Minimal | Grows until compacted | Simple |
| **Bitcask** | Append raw key/value entries across numbered data files | Uses an index for direct lookup, enabling faster restarts | Index proportional to key count | Compaction keeps only latest values | Higher |
| **Hybrid log** | Buffer writes in memory then append to a single log file | Scans log to rebuild index | Buffer size plus key index | Single file grows until compacted | Moderate |


## Running Tests

Run all unit tests with:

```bash
dotnet test Src/Nemcache.Tests/Nemcache.Tests.csproj
dotnet test Src/Nemcache.DynamoService.Tests/Nemcache.DynamoService.Tests.csproj
```
