# Katzebase : Client Connectivity Library

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/Katzebase.Api

Katzebase is an ACID compliant document-based database written in C# using .NET 10 that runs on Windows or Linux. By default it runs as a service but the libraries can also be embedded. It supports what you'd expect from a typical relational-database-management-system except the "rows" are stored as sets of key-value pairs (called documents) and the schema is not fixed. The default engine is wrapped by [ReliableMessageing](https://github.com/NTDLS/NTDLS.ReliableMessaging) controllers and allows access via APIs , a t-SQL like syntax, or by using the bundled management UI (which just calls the APIs).

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

## Usage

```csharp
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;

using var client = new KbClient("localhost", 6858, "admin", KbClient.HashPassword(""));

client.Schema.CreateRecursive("Sales:Orders");

// Documents by id.
uint id = client.Document.Store("Sales:Orders", new { Customer = "Contoso", Total = 125.50 });
var order = client.Document.Get<Order>("Sales:Orders", id);
client.Document.Replace("Sales:Orders", id, new { Customer = "Contoso", Total = 99.00 });
client.Document.Delete("Sales:Orders", id);

// Queries, with parameters (an anonymous object or a dictionary), mapped to objects.
List<Order> orders = client.Query.Fetch<Order>(
    "SELECT * FROM Sales:Orders WHERE Customer = @Customer", new { Customer = "Contoso" });

// Transactions: disposing the scope rolls back unless it was committed.
using (var transaction = client.Transaction.Begin())
{
    client.Document.StoreMany("Sales:Orders", newOrders);
    transaction.Commit();
}

// Every method has an async version.
var count = await client.Query.FetchScalarAsync<int>("SELECT Count(0) FROM Sales:Orders");

// Errors are thrown as the same exception type the server raised.
try
{
    client.Document.Store("Sales:Orders", new { OrderNumber = 1 });
}
catch (KbDuplicateKeyViolationException) { /* a unique key was violated */ }
catch (KbDeadlockException) { /* the transaction was rolled back, retry it */ }
catch (KbTimeoutException) { /* the server did not reply in time */ }
catch (KbConnectionException) { /* not connected, or the connection was lost */ }
```

A client holds a single session, and a session has at most one transaction: requests made through the same client (from any
thread) share it. Use one client per independent unit of work.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License

[MIT](https://choosealicense.com/licenses/mit/)
