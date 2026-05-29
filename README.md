# Katzebase
![Logo128](https://github.com/NTDLS/NTDLS.Katzebase.Server/assets/11428567/fa827156-4d19-4803-860f-aa0ef3a5151d)

Katzebase is an ACID compliant document-based database written in C# using .NET 9 that runs on Windows or Linux. By default it runs as a service but the libraries can also be embedded. It supports what you'd expect from a typical relational-database-management-system except the "rows" are stored as sets of key-value pairs (called documents) and the schema is not fixed. The default engine is wrapped by [ReliableMessageing](https://github.com/NTDLS/NTDLS.ReliableMessaging) controllers and allows access via APIs , a t-SQL like syntax, or by using the bundled management UI (which just calls the APIs).

## Testing Status
[![Regression Tests](https://github.com/NTDLS/Katzebase/actions/workflows/%20Regression%20Tests.yaml/badge.svg)](https://github.com/NTDLS/Katzebase/actions/workflows/%20Regression%20Tests.yaml)

## Documentation and Links
- **Full documentation** at [https://katzebase.com/](https://katzebase.com/).
- To download the **Server**, **Management UI**, and utilities, check out the [releases](https://github.com/NTDLS/Katzebase/releases).
- Looking for the installer for an older version? They are archived at https://networkdls.com/Software/View/Katzebase

## Default Login
 - **Username**: admin
 - **Password**: \<blank\>

## Features:
- Abortable transactions.
- Caching and write deferment.
- Locking, isolation and atomicity.
- Indexing with partitioning.
- Multi and nested schemas with partitioning.
- Static analyzer and schema aware UI.
- Logging and health monitoring.
- Simple to use API client and DAPPER like querying.
- tSQL Query language with support for field list, joins, top(count), where clause, grouping, aggregations, etc.

## Client Connectivity?
Grab the [nuget package](https://www.nuget.org/packages/NTDLS.Katzebase.Api/) for your project over at nuget.org.

## Sample Data
To run the included examples, download the [sample Katzebase database]( https://katzebase.com/Download/Katzebase.zip), which is a compressed archive containing a word list and various relationsips between the words and languages.
If you are feeling more ambitious, you can grab the larger [Katzebase with StackOverflow](https://katzebase.com/WWWRoot/Download/Katzebase%20with%20StackOverflow.7z) database.

# Management UI
![image](https://github.com/user-attachments/assets/6e6f337c-e30c-436c-94bd-182211e4054a)

## SQL Server Migration Tool
We even included a tool to import your schema, data and indexes from SQL Server into Katzebase.

![image](https://github.com/user-attachments/assets/88f99e45-adc1-40e2-a6b2-2cd1776f8716)

## Technologies
---

### ![Reliable Messaging](https://katzebase.com/Page/Image/home/reliablemessaging_png) Reliable Messaging
[NTDLS.ReliableMessaging](https://github.com/NTDLS/NTDLS.ReliableMessaging) provides lightweight, simple, and high-performance TCP/IP based inter-process-communication / RPC functionality.

---

### ![Delegate Thread Pooling](https://katzebase.com/Page/Image/home/delegatethreadpooling_png) Delegate Thread Pooling
[NTDLS.DelegateThreadPooling](https://github.com/NTDLS/NTDLS.DelegateThreadPooling) is a high performance and predictable active thread pool where work items can be queued as delegate functions. Allowing for infinite FIFO worker items or enforce queue size, wait on collections of those items to complete, and total control over the pool size. Also allows for multiple pools, so that different workloads do not interfere with one another.

---

### ![Semaphore](https://katzebase.com/Page/Image/home/semaphore_png?Scale=) Semaphore
[NTDLS.Semaphore](https://github.com/NTDLS/NTDLS.Semaphore) provides various classes to protect a variable from parallel / non-sequential thread access by always acquiring an exclusive lock on the resource. Also allows for shared access, pessimistic locking, optimistic locking and dead-lock prevention lock patterns with lightweight cancellation.

---

### ![Expression Parser](https://katzebase.com/Page/Image/home/expressionparser_png) Expression Parser
[NTDLS.ExpressionParser](https://github.com/NTDLS/NTDLS.ExpressionParser) is a mathematics parsing engine. It supports expression nesting, custom variables, custom functions all standard mathematical operations for integer, decimal (floating point), logic and bitwise. It sets at the core of all Katzebase condition matches.

Benchmarked at ~0.22 µs per expression (~4.5M eval/s per core). That's ~22M arithmetic ops/sec with our test expression. Roughly on par with compiled expression trees / LLVM-JIT math engines — this parser isn't "fast for C#"; it's fast, period.

---

### ![Fast Memory Cache](https://katzebase.com/Page/Image/home/fastmemorycache_png) Fast Memory Cache
[NTDLS.FastMemoryCache](https://github.com/NTDLS/NTDLS.FastMemoryCache) provides fast and easy to use thread-safe partitioned memory cache for C# that helps manage the maximum size and track performance.

---

### ![Secure Key Exchange](https://katzebase.com/Page/Image/home/securekeyexchange_png) Secure Key Exchange
[NTDLS.SecureKeyExchange](https://github.com/NTDLS/NTDLS.SecureKeyExchange) allows the server to generate single or multi-round Diffie-Hellman keys in C#.

---

### ![Persistence](https://katzebase.com/Page/Image/home/persistence_png) Persistence
[NTDLS.Persistence](https://github.com/NTDLS/NTDLS.Persistence) are helpers for reading and writing serialized objects to/from files. Helpful for configuration files.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License
[MIT](https://choosealicense.com/licenses/mit/)
