# Short Term
- Add queue listeners and workflow completion events
- Add cyclic dependency checks
- Validate locking on dequeue operation to minimise chance of race conditions
- Evaluate locking strategies, remove need for common global locks, reduce the problem down to between queue and workflow items

# Long Term
- Persist all collections to disk
 - Persisted memory mapping
 - Lucene.Net indexes
 - Create high-level object model abstractions
- Look at mirroring for high availability - master, multi-slave setups
- Add transaction logging and crash recovery
