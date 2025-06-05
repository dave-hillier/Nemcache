# Using a Bitcask Style Store

This document outlines how Nemcache could adopt a Riak Bitcask inspired persistence layer.

Bitcask is an append-only log structured key/value store. Each update is written to the end of an active data
file, and an in-memory index maps keys to the latest value position. Periodic compaction merges active entries
into new files to reclaim space.

Nemcache currently persists cache activity in `StreamArchiver` using protobuf messages. A bitcask approach would
change this to writing raw key/value entries with offsets stored in a directory. Startup would rebuild the index
by scanning the log or reading hint files.

## Possible Integration Steps

1. **Data files** – Maintain numbered data files (e.g. `data.1`, `data.2` …) in a persistence folder. Append records
   consisting of a small header followed by the key and value bytes.
2. **In-memory directory** – On write, update a dictionary mapping each key to a tuple of file identifier, offset and
   record length. On read, look up the entry and seek directly to the value in the file.
3. **Compaction** – When old files contain mostly obsolete entries, rewrite the latest values into a new data file and
   discard the old ones. Nemcache’s existing `StreamArchiver` compact logic can be reused here.
4. **Recovery** – At startup, scan data files from oldest to newest to rebuild the directory. A separate hint file can
   store the directory state to speed up recovery if desired.
5. **Integration with `MemCache`** – Replace the current `StreamArchiver` observer with a `BitcaskStore` that performs
   these operations. Reads and writes in `MemCache` remain unchanged but persistence uses the new store.

This approach keeps writes sequential and allows quick recovery of the cache state. The existing eviction and
notification mechanisms remain untouched, while persistence gains the efficiency of Bitcask’s design.

The Bitcask store is optional. Set the environment variable `NEMCACHE_USE_BITCASK=1` when starting the service to
use this mode instead of the default in-memory log.
