Persistence

Something for managing:
* creating and subscribing log files
* clean up old log files

Make it an optional field for cache entries

Client API
* Subscriptions
* Subscription groups of keys
* Groups of clients?? Entries with different views for different clients? 
* Add a new client API
* Auth??

REST API

// TODO

        // TODO: extra flags. Proxied, Persistent, Transient, Replicated


Cache replication

* Connect another cache and have it mirror the master
* Needs a way of subscribing after start. How to ensure that they're the same?
* Local simulation then upgrade to over network
* Quorum?

Subscriptions for keys
* managing interest?
* Keys are independent - a version might be useful

Mode for keys. Are they proxied? Persistent? Transient?

Authentication
* how do I control access to keys? Build using same 

Distribution
* Share the caching space based on a hashing
Load balance?

Persistence
* Works similar to above except mirror to a journal
* Restore at startup?
* Compacting

Atomic writes?
* Do a DB like scheme with 

Storing Complex structures?


