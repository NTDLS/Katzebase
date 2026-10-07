# Release Notes

## Performance: transactions, locking and indexing

This release targets insert and select throughput. Single statements are now up to ~100× faster, concurrent workloads up to ~200× faster, and several index consistency bugs have been fixed.

### Highlights

| Scenario | Before (ops/s) | After (ops/s) | Change |
|---|---:|---:|---:|
| Single-row `INSERT` (implicit transaction) | 33 | 3,780 | ~115× |
| Bulk `INSERT` (rows/s, 1,000-row statements) | 6,631 | 13,377 | ~2× |
| `SELECT` by unique key | 34 | 3,116 | ~92× |
| `SELECT` by non-unique index (~230 rows) | 30 | 186 | ~6× |
| `SELECT` by composite index | 24 | 228 | ~9× |
| Concurrent `SELECT` by unique key (8 threads) | 29 | 6,957 | ~240× |
| 1 writer + 8 concurrent readers | 28 | 6,324 | ~225× |
| `UPDATE` indexed column by unique key | 31 | 2,666 | ~86× |

*Engine benchmark, median of three runs on the same machine, statements pre-parsed so parser time is excluded. 23,000 documents with a non-unique, a composite and a unique index.*

---

### Transactions

- **Every transaction no longer creates and drops its own RocksDB column family.** Doing so forced RocksDB to rewrite its MANIFEST, which took ~22 ms and was serialized across the whole database. Because every statement runs in a transaction, this capped the server at roughly 40 statements per second regardless of load or thread count. The undo log now lives in one shared `TransactionAtoms` column family, keyed by transaction id and sequence. The overhead of an empty transaction dropped from ~22 ms to ~0.13 ms.
- Transaction log sequence numbers are generated in memory, replacing a RocksDB read and write per logged change.
- The name of the API operation is now captured at compile time, so a stack trace is no longer captured on every statement.

### Locking

- **The lock table is now indexed** by object, path and recursive path. Finding conflicting locks no longer scans every lock held in the engine.
- **There is a new fast path for already-covered locks.** A lock request that is already satisfied by a lock the transaction holds (for example, a document inside a schema it has locked) only needs a shared read of the lock table. The exclusive path is skipped.
- **Blocked lock requests now wait instead of spinning.** They sleep until a lock is released, or for at most 25 ms so that deadlock detection, lock-wait timeouts and cancellation still run. Previously, waiting transactions repeatedly took the global lock-management semaphore and starved every other transaction.
- Transactions are only registered as "waiting" once they are actually blocked.
- The redundant per-file grant semaphores were removed.
- Health counters are now lock-free on the hot path. Every IO read, cache hit and lock grant previously took a global write lock just to increment a counter.

### Indexing

- **The index catalog is cached.** It was read from RocksDB and JSON-deserialized for every inserted, updated or deleted document and for every query. It is now cached per schema and invalidated by `CREATE INDEX`, `DROP INDEX` and `REBUILD INDEX`, including on commit and rollback.
- **Multiple indexes are now intersected.** Previously, when a condition group could use more than one index, only the last lookup chosen was kept, which was the least selective one. Lookups are now ranked (unique point lookup, then point lookup, then prefix seek, then full index scan) and the cheap ones are intersected.
- **Composite indexes seek on all leading equality columns.** Previously only the first column was used. When every column is constrained by an equality, the lookup is a single key read.
- Condition values are resolved once per lookup instead of once per scanned index key.
- `UPDATE` skips index maintenance for any index whose values did not change.
- `EXPLAIN` output lists every index used for a condition group and notes when the indexes are intersected.

### Client/server protocol and logging

- **Compression has been removed from the client/server connection.** Deflate compression of every message cost more time than it saved. Single-row inserts through the client are 10–20% faster without it (2,167 → 2,657 rows/s inside an explicit transaction).
- **Trace logging no longer captures a stack trace when tracing is disabled.** Every trace call captured a full stack trace to name the caller, even when verbose logging was off, and the engine traces every document read. Reads and updates are 30–55% faster as a result. For example, point selects went from 3,116 to 4,601 per second, and concurrent point selects from 6,957 to 10,778.

---

### Bug fixes

- **Index entries are now part of the transaction log.** Previously they were written outside the log, which caused these problems:
  - A rolled-back or failed `INSERT` (including one that failed on a unique-key violation) left index entries pointing at documents that no longer existed. Later indexed queries failed with `Document with ID [n] does not exist`, and unique values remained taken.
  - A rolled-back `DELETE` did not restore its index entries, so the document disappeared from index lookups.
- **`UPDATE` did not remove documents from their old index entries.** The document was already modified when the old entry was looked up, so the stale entry remained. After updating a unique column, the old value could never be inserted again.
- **Concurrent `UPDATE`s on the same schema deadlocked.** `UPDATE` took a schema read lock and then document write locks. Each transaction's read lock blocked the others' write locks, which is a lock-upgrade deadlock. In testing, more than half of concurrent updates were terminated as deadlock victims.
- `UPDATE`s that named a column with different casing than the index definition (e.g. `SET sub = …` on an index over `Sub`) skipped index maintenance.
- When deferred IO was enabled, reading a list of objects (such as the index catalog) skipped items not modified by the current transaction.
- `IndexSelection.Clone()` copied covered conditions into itself instead of the clone.
- Debug builds failed to compile due to unqualified types in `SystemShowAggregateFunctions`, `SystemShowScalarFunctions` and `SystemShowSystemFunctions`.

---

### Breaking changes

- **The wire protocol has changed: client/server compression has been removed.** Clients and servers from this release cannot communicate with clients or servers from earlier releases. Upgrade the server and every application that uses `NTDLS.Katzebase.Api` (including the management tools) together.

### Behavior changes

- **`UPDATE` now acquires a write lock on the target schema up front**, the same way `INSERT` does. Concurrent updates to the same schema now run one after another instead of deadlocking. Updates to different schemas are unaffected.
- `SHOW WAITING LOCKS` now only lists transactions that are actually blocked, not every in-flight lock request.

### Upgrade notes

- **Upgrade the server and all clients at the same time.** See *Breaking changes*.
- **The on-disk transaction log format has changed.** Shut the server down cleanly before upgrading. Transactions that were left open by a crash of a previous version are not rolled back by this version.
- No changes to document, schema or index storage formats are required. Existing indexes do not need to be rebuilt.
  - Exception: indexes that have been affected by the bugs above may contain stale entries. Run `REBUILD INDEX` on them to remove those entries.

### Testing

- Added `TestIndexConsistency`. It covers rolled-back inserts, updates and deletes; failed inserts; updates that move index entries; queries using multiple indexes; and concurrent updates.
- Added the `InsertBenchmark` test application. It measures single-row inserts through the client API, both without an explicit transaction and inside an explicit transaction committed every N rows. By default it hosts its own server in-process; `--server host:port` targets an existing server instead. Run it with `--help` for all options.
