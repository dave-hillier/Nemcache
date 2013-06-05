In no particular order:

- [x] Implement web socket/subscription handler
- [ ] Extract the persistent store to self contained library. Create tests around persistent behaviour. Change interface to be more like Dictionary?
	- Perhaps persistent dictionary first. Then persistent cache on top of that. 
- [ ] Queuing!
- [ ] Examples of HTTP features
- [ ] Examples of Websocket subscription features
- [ ] Master slave replication, then more advanced replication
- [ ] Examples of WebSocket queuing
- [ ] Fix up the client
- [ ] Configuration of the server - perhaps autofac or similar...
- [ ] Consider using the message queue of the archiver to do the compacting.
- [ ] Metadata for individual keys. Probably need to be persistent - but might be worth turning off persistence for keys.
- [ ] WebSocketServer alternative protocol for the CacheEntrySubscriptionHandler. Perhaps decouple from JSON in the CacheEntrySub...
- [ ] Finish off the static file server
- [ ] Proxying client
- [ ] Any rationalisation between the Memcache protocol and REST? To hopefully make it easier to develop protocols.