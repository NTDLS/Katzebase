# Katzebase
![Icon128](https://github.com/NTDLS/NTDLS.Katzebase.Server/assets/11428567/36bf8f56-0500-41b4-93de-044a062b0911)

- **Server / Service** code and releases: https://github.com/NTDLS/NTDLS.Katzebase.Server
- **Client Connectivity Libraries** code and releases: https://github.com/NTDLS/NTDLS.Katzebase.Client
- **SQL Server Migration Tool** code and releases: https://github.com/NTDLS/NTDLS.Katzebase.SQLServerMigration
- **Management UI** code and releases: https://github.com/NTDLS/NTDLS.Katzebase.Management

Katzebase is an ACID compliant document-based database written in C# using .NET 8 that runs on Windows or Linux. By default it runs as a service but the libraries can also be embedded. It supports what you'd expect from a typical relational-database-management-system except the "rows" are stored as sets of key-value pairs (called documents) and the schema is not fixed. The engine is wrapped by MVC controllers and allows access via APIs, a t-SQL like syntax, or by using the bundled management UI (which just calls the APIs).

![image](https://github.com/NTDLS/Katzebase/assets/11428567/02899c13-1eab-4b86-8e3d-601efc2a419d)

## Documentation
Check out the full documentation at [https://katzebase.com/](https://katzebase.com/).


## Features:
- Abortable transactions.
- Caching and write deferment.
- Locking, isolation and atomicity.
- Document indexing.
- Partitioning.
- Logging and health monitoring.
- SQL Query language with support for (field list, joins, top(count), where clause).


## Client Connectivity?
Grab the [nuget package](https://www.nuget.org/packages/NTDLS.Katzebase.Client/) for your project over at nuget.org.
Or, maybe you are just looking for the [client source code](https://github.com/NTDLS/NTDLS.Katzebase.Client)?


## Sample Data
To run the included examples, download the [sample Katzebase database]( https://katzebase.com/Download/Katzebase.zip), which is a compressed archive containing a word list and various relationsips between the words and languages.


## SQL Server Migration Tool
We even included a tool to import your schema and data from SQL Server into Katzebase.


![image](https://github.com/NTDLS/NTDLS.Katzebase/assets/11428567/41959624-0254-4566-a495-05c72f4a3642)


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License
[MIT](https://choosealicense.com/licenses/mit/)
