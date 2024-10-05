# Katzebase : Client Connectivity Library

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/Katzebase.Api

Katzebase is an ACID compliant document-based database written in C# using .NET 8 that runs on Windows or Linux. By default it runs as a service but the libraries can also be embedded. It supports what you'd expect from a typical relational-database-management-system except the "rows" are stored as sets of key-value pairs (called documents) and the schema is not fixed. The default engine is wrapped by [ReliableMessageing](https://github.com/NTDLS/NTDLS.ReliableMessaging) controllers and allows access via APIs , a t-SQL like syntax, or by using the bundled management UI (which just calls the APIs).

## Documentation and Links
- **Full documentation** at [https://katzebase.com/](https://katzebase.com/).
- To download the **Server**, **Management UI**, and utilities, check out the [releases](https://github.com/NTDLS/Katzebase/releases).

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

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License

[MIT](https://choosealicense.com/licenses/mit/)
