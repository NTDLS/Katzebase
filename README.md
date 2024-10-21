# Katzebase
![Logo128](https://github.com/NTDLS/NTDLS.Katzebase.Server/assets/11428567/fa827156-4d19-4803-860f-aa0ef3a5151d)

Katzebase is an ACID compliant document-based database written in C# using .NET 8 that runs on Windows or Linux. By default it runs as a service but the libraries can also be embedded. It supports what you'd expect from a typical relational-database-management-system except the "rows" are stored as sets of key-value pairs (called documents) and the schema is not fixed. The default engine is wrapped by [ReliableMessageing](https://github.com/NTDLS/NTDLS.ReliableMessaging) controllers and allows access via APIs , a t-SQL like syntax, or by using the bundled management UI (which just calls the APIs).

## Testing Status
[![Run All NetCore](https://github.com/NTDLS/Katzebase/actions/workflows/RunAllNetCore.yaml/badge.svg)](https://github.com/NTDLS/Katzebase/actions/workflows/RunAllNetCore.yaml)

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

## Client Connectivity?
Grab the [nuget package](https://www.nuget.org/packages/NTDLS.Katzebase.Api/) for your project over at nuget.org.

## Sample Data
To run the included examples, download the [sample Katzebase database]( https://katzebase.com/Download/Katzebase.zip), which is a compressed archive containing a word list and various relationsips between the words and languages.
If you are feeling more ambitious, you can grab the larger [Katzebase with StackOverflow](https://katzebase.com/WWWRoot/Download/Katzebase%20with%20StackOverflow.7z) database.

# Management UI
![image](https://github.com/user-attachments/assets/6e6f337c-e30c-436c-94bd-182211e4054a)

## SQL Server Migration Tool
We even included a tool to import your schema, data and indexes from SQL Server into Katzebase.

![image](https://github.com/user-attachments/assets/8bbbc47a-78b1-47f0-8fbb-c44962482d22)

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License
[MIT](https://choosealicense.com/licenses/mit/)
