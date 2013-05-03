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


