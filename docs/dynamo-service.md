# Dynamo-like Service

`Nemcache.DynamoService` is an Orleans-based host that stores partition data using the existing persistent `MemCache` implementation.

The project demonstrates how Nemcache can integrate with Orleans to build a Dynamo-inspired architecture. Each partition is represented by a grain and writes are persisted using the Nemcache storage engine.

Grains replicate each write to two additional replicas to mimic Dynamo's N-way replication.

Keys are routed to partitions using a simple consistent hashing ring so that data remains evenly distributed if the number of partitions changes.

Run the service with:

```bash
cd Src
dotnet run --project Nemcache.DynamoService
```
