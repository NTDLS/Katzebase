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
- **Transaction log entries are stored in a compact binary format** instead of JSON. One entry is written for every key a transaction creates, alters or deletes.
- The name of the API operation is now captured at compile time, so a stack trace is no longer captured on every statement.

### Locking

- **The lock table is now indexed** by object, path and recursive path. Finding conflicting locks no longer scans every lock held in the engine.
- **There is a new fast path for already-covered locks.** A lock request that is already satisfied by a lock the transaction holds (for example, a document inside a schema it has locked) only needs a shared read of the lock table. The exclusive path is skipped.
- **Blocked lock requests now wait instead of spinning.** They sleep until a lock is released, or for at most 25 ms so that deadlock detection, lock-wait timeouts and cancellation still run. Previously, waiting transactions repeatedly took the global lock-management semaphore and starved every other transaction.
- Transactions are only registered as "waiting" once they are actually blocked.
- The redundant per-file grant semaphores were removed.
- Health counters are now lock-free on the hot path. Every IO read, cache hit and lock grant previously took a global write lock just to increment a counter.

### Indexing

- **New index storage layout: one entry per document.** Previously each distinct indexed value had one entry holding the packed list of every matching document id. Every insert or delete then read and rewrote the whole list, so maintaining an index on a column with few distinct values got slower as the table grew. In testing, an index on a two-value column dropped from ~5,600 to ~700 rows/s by 200,000 rows. Each document now has its own entry, so inserts and deletes are a single small write; the same test stays flat at ~30,000 rows/s.
  - Non-unique indexes are keyed by the values plus the document id. Unique indexes are keyed by the values alone, with the document id as the value, so unique lookups and uniqueness checks are a single key read.
- **Bloom filters are enabled on all RocksDB tables.** Lookups for keys that don't exist, such as the existence check on every insert, can skip reading data from disk. They apply to newly written data files; existing files gain them as RocksDB compacts.
- **The index catalog is cached.** It was read from RocksDB and JSON-deserialized for every inserted, updated or deleted document and for every query. It is now cached per schema and invalidated by `CREATE INDEX`, `DROP INDEX` and `REBUILD INDEX`, including on commit and rollback.
- **Multiple indexes are now intersected.** Previously, when a condition group could use more than one index, only the last lookup chosen was kept, which was the least selective one. Lookups are now ranked (unique point lookup, then point lookup, then prefix seek, then full index scan) and the cheap ones are intersected.
- **Composite indexes seek on all leading equality columns.** Previously only the first column was used. When every column is constrained by an equality, the lookup is a single key read.
- Condition values are resolved once per lookup instead of once per scanned index key.
- `UPDATE` skips index maintenance for any index whose values did not change.
- `EXPLAIN` output lists every index used for a condition group and notes when the indexes are intersected.

### Bulk inserts

- **New `client.Document.StoreMany(schema, documents)` API.** It sends a batch of documents to the server in one round trip and stores them in a single atomic transaction. It returns the new document ids in the order they were given. With batches of 1,000 documents, inserts run at about 14,000 rows/s with three indexes and about 28,000 rows/s with none. That compares to about 4,100 rows/s with one `Document.Store` call per row.
- **`NTDLS.Katzebase.SQLServerMigration` now imports data with `StoreMany`**, in batches of up to 1,000 rows or 4 MB.

### Client API (NTDLS.Katzebase.Api)

- **Typed exceptions.** Server errors used to reach the client as plain `System.Exception`, so callers could only tell them apart by message text. They are now rethrown as the same type the server raised (`KbDuplicateKeyViolationException`, `KbObjectNotFoundException`, `KbDeadlockException`, `KbParserException` with its line number, ...). Timeouts throw `KbTimeoutException`, and requests when not connected (or when the connection is lost) throw the new `KbConnectionException`.
- **New document operations by id:** `Document.Get`, `Get<T>`, `Replace` (replaces the whole document and updates indexes), `Delete` and `DeleteMany`. `Document.Store` now returns the new document's id.
- **Async methods.** Every operation has an `...Async` version that accepts a `CancellationToken`.
- **Transaction scopes.** `Transaction.Begin()` returns a scope; disposing it rolls the transaction back unless `Commit()` was called.
- **Schema `Attach`/`Detach`** methods for `ATTACH SCHEMA` and `DETACH SCHEMA`.
- **Simpler parameters.** Each query method takes one `parameters` argument: an anonymous object, any object, or a dictionary of names to values (with or without the leading `@`). Values are sent culture-invariantly, so `1.5` is no longer sent as `"1,5"` on machines using a comma decimal separator. Booleans are sent as `1`/`0` and dates in ISO 8601.
- **More robust result mapping** (`Fetch<T>`, `MapTo<T>`). Values are parsed culture-invariantly, booleans accept `1`/`0`, enums and nullable types are supported, and read-only properties are skipped instead of failing.
- `Dispose()` no longer throws when the server is unreachable, and `Disconnect()` is now public.

### Client/server protocol and logging

- **Compression has been removed from the client/server connection.** Deflate compression of every message cost more time than it saved. Single-row inserts through the client are 10–20% faster without it (2,167 → 2,657 rows/s inside an explicit transaction).
- **Trace logging no longer captures a stack trace when tracing is disabled.** Every trace call captured a full stack trace to name the caller, even when verbose logging was off, and the engine traces every document read. Reads and updates are 30–55% faster as a result. For example, point selects went from 3,116 to 4,601 per second, and concurrent point selects from 6,957 to 10,778.

---

### New: ATTACH SCHEMA and DETACH SCHEMA

Namespaces (a schema with all of its documents, indexes, policies and child schemas) can now be moved between databases and servers.

```sql
-- Remove a namespace from this database and move its files to a folder.
DETACH SCHEMA Sales:Archive TO 'D:\Exports\SalesArchive'

-- Copy a namespace from a folder into this (or another) database, under any name.
ATTACH SCHEMA Sales:Archive FROM 'D:\Exports\SalesArchive'
```

- **`ATTACH SCHEMA <schema> FROM '<folder>'`** copies the folder into the database as a new schema; the source folder is never modified.
  - Each schema in the attached namespace gets a new id, so the same folder can be attached more than once.
  - The statement's output reports any indexes created by an older version of Katzebase, which must be rebuilt before use.
- **`DETACH SCHEMA <schema> TO '<folder>'`** removes the schema from the database and moves its folder out. It waits for other transactions using the schema to finish.
- The folder can be a quoted string or a string variable. It must be an absolute path that does not yet exist (for `DETACH`), and it must not be inside the server's data or transaction folders.
- Both statements require an administrator, because they read and write arbitrary folders on the server. They can't be used inside an explicit transaction, because moving files can't be undone by the transaction log.
- Failures never leave a partially attached or detached namespace:
  - `ATTACH` copies into a staging folder, checks that every schema in it can be opened, then renames the folder into place.
  - `DETACH` unregisters the schema before moving its files, and puts them back if the move fails.
- Folder paths cannot end with a backslash (`'D:\Exports\'`), because a trailing backslash escapes the closing quote.

### Management UI

- **Create Index** (right-click a schema or its *Indexes* folder) now opens a ready-to-edit `CREATE INDEX` script for the schema, listing the schema's known fields.
- **Find in Server Explorer** (right-click an editor tab) now selects, in the server explorer, the schema referenced by the statement under the cursor, or the tab's server if the script doesn't reference one.
- Opening a recent file that no longer exists now says so, and removes it from the *Recent Files* menu immediately.

### Bug fixes

- Reading a document by key after it was updated or deleted could return the stale cached version. Writes and deletes now evict the cached copy.
- `KbProcedure(schemaName, procedureName)` assigned the schema and procedure names to each other.

- **Creating or rebuilding an index leaked a RocksDB column family handle.** Dropping a column family (which `CREATE INDEX`, `REBUILD INDEX`, `DROP INDEX` and transaction rollback all do) never destroyed its handle. That kept part of the schema's database open after it was closed, so the schema's folder could not be moved or renamed until the server restarted.

- **Index entries are now part of the transaction log.** Previously they were written outside the log, which caused these problems:
  - A rolled-back or failed `INSERT` (including one that failed on a unique-key violation) left index entries pointing at documents that no longer existed. Later indexed queries failed with `Document with ID [n] does not exist`, and unique values remained taken.
  - A rolled-back `DELETE` did not restore its index entries, so the document disappeared from index lookups.
- **`UPDATE` did not remove documents from their old index entries.** The document was already modified when the old entry was looked up, so the stale entry remained. After updating a unique column, the old value could never be inserted again.
- **Concurrent `UPDATE`s on the same schema deadlocked.** `UPDATE` took a schema read lock and then document write locks. Each transaction's read lock blocked the others' write locks, which is a lock-upgrade deadlock. In testing, more than half of concurrent updates were terminated as deadlock victims.
- `UPDATE`s that named a column with different casing than the index definition (e.g. `SET sub = …` on an index over `Sub`) skipped index maintenance.
- When deferred IO was enabled, reading a list of objects (such as the index catalog) skipped items not modified by the current transaction.
- **SQL Server migration could silently lose rows.** Any error other than a deadlock was swallowed and the row was skipped. A deadlock rolled back the whole open transaction (up to 10,000 rows), but only the current row was retried. Now each batch is atomic, so a deadlocked batch is retried in full (up to 10 times with backoff). Any other error stops the import of that table, and the error message is shown in the grid.
- `ANALYZE SCHEMA ... WITH (IncludePhysicalPages = true)` reported every non-unique index as having no documents. It now counts the new one-entry-per-document layout correctly, and reports indexes in the old format as needing a rebuild.
- `IndexSelection.Clone()` copied covered conditions into itself instead of the clone.
- **The server failed to start when a data folder was configured with a Windows 8.3 short name** (such as `C:\Users\JPATTE~1\...`). Some paths were expanded to their long form and others weren't, so the same RocksDB database was opened twice. Configured folders are now always stored as full, long paths.
- **Scalar and aggregate function fixes:**
  - `IsInteger()` returned the opposite result (0 for `'15'`, 1 for `'1.5'`). It now also accepts integers larger than 32 bits.
  - `CountDistinct(values, caseSensitive)` had its flag reversed. It now ignores case by default and compares case-sensitively when the flag is true.
  - `IsEmpty()`, `IsNumeric()`, `NullWhen()` and `NullWhenNumeric()` were listed by `ShowScalarFunctions` but failed with "not implemented". They now work, and `IsEmpty()` and `IsNumeric()` return `1`/`0` like the other boolean functions.
- `EXEC` with a name that isn't a system procedure failed with "Reimplement user procedures". It now throws `KbObjectNotFoundException` ("Procedure not found"), as does `Procedure.Execute` in the client API.
- **Removed the `DocumentID()` scalar function.** It was left over from the page-based storage engine and always returned null. Document ids are returned by the client API.
- Debug builds failed to compile due to unqualified types in `SystemShowAggregateFunctions`, `SystemShowScalarFunctions` and `SystemShowSystemFunctions`.

---

### Breaking changes

- **Client API changes:**
  - Query and procedure methods take a single `object? parameters` argument instead of separate object/dictionary overloads. Pass timeouts by name (`queryTimeout:`) or using the `(statement, TimeSpan)` overloads. A `TimeSpan` passed as parameters is rejected.
  - Procedures are identified by their fully qualified name (`"Schema:Procedure"`); the separate schema/procedure-name overloads were removed.
  - `Schema.Indexes.Get` returns the `KbIndex` (or null) and `Schema.Indexes.List` returns `List<KbIndex>`, rather than reply objects.
  - `Fetch<T>` and friends return `List<T>`.
  - `Server.StartSession` and `Server.CloseSession` are internal; use `Connect` and `Disconnect`.
  - Parameters are sent with different representations: booleans as `1`/`0`, dates in ISO 8601, numbers in the invariant culture.
  - **`COMMIT` (and `Transaction.Commit()`) with no open transaction is now an error** (`KbTransactionCancelledException`). It used to silently succeed, so a transaction the server had rolled back, e.g. as a deadlock victim, looked committed.

- **The wire protocol has changed: client/server compression has been removed.** Clients and servers from this release cannot communicate with clients or servers from earlier releases. Upgrade the server and every application that uses `NTDLS.Katzebase.Api` (including the management tools) together.
- **The index storage format has changed. Every existing index must be rebuilt** with `REBUILD INDEX <name> ON <schema>` after upgrading. Until it is rebuilt, an index:
  - is ignored by queries, which fall back to scanning documents, so results stay correct but are slower;
  - causes inserts, updates and deletes on its schema to fail with an error naming the index and the command to run.

### Behavior changes

- **`UPDATE` now acquires a write lock on the target schema up front**, the same way `INSERT` does. Concurrent updates to the same schema now run one after another instead of deadlocking. Updates to different schemas are unaffected.
- `SHOW WAITING LOCKS` now only lists transactions that are actually blocked, not every in-flight lock request.

### Upgrade notes

- **Upgrade the server and all clients at the same time.** See *Breaking changes*.
- **The on-disk transaction log format has changed.** Shut the server down cleanly before upgrading. Transactions that were left open by a crash of a previous version are not rolled back by this version.
- **Rebuild every index** (`REBUILD INDEX <name> ON <schema>`) after upgrading. See *Breaking changes*. Rebuilding also removes any stale entries left by the index bugs fixed in this release.
- Document and schema storage formats are unchanged.

### Testing

- Added `TestIndexConsistency`. It covers rolled-back inserts, updates and deletes; failed inserts; updates that move index entries; queries using multiple indexes; and concurrent updates.
- Added `TestAttachDetach`. It covers detaching and re-attaching a namespace (documents, indexes and child schemas), attaching the same folder twice, reporting outdated indexes, and invalid or unsafe requests.
- Added the `InsertBenchmark` test application. It measures inserts through the client API in four ways: single-row SQL `INSERT`s without an explicit transaction, the same inside an explicit transaction committed every N rows, `Document.Store` per row, and `Document.StoreMany` in batches. By default it hosts its own server in-process; `--server host:port` targets an existing server instead. Run it with `--help` for all options.
