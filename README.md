# Katzebase
![Catzebase](https://github.com/NTDLS/Katzebase/assets/11428567/99a11cfb-bbd7-468a-b9d6-7f5fe772aacd)

Katzebase is a document-based database written in .net 7 that runs on Windows or Linux. By default It can run as a service but the libraries can also be embedded. It basically supports what you’d expect from a typical relational-database-management-system except the "rows" are stored as json and the schema is not fixed. The engine is wrapped by MVC controllers and allows access via APIs, a SQL like syntax or by using the bundled management UI (which calls the APIs).

![image](https://github.com/NTDLS/Katzebase/assets/11428567/02899c13-1eab-4b86-8e3d-601efc2a419d)

## Features:
- Abortable transactions.
- Caching and write deferment.
- Locking, isolation and atomicity.
- Document indexing.
- Partitioning.
- Logging and health monitoring.
- SQL Query language with support for (field list, joins, top(count), where clause).

## Sample Data
You can download the [sample Katzebase database](https://networkdls.com/DirectDownload/Katzebase/AdventureWorks.7z), which is a dump of the AdventureWorks2012 SQL Server database. Yea, I know its old - but thats the database I had on hand.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License

[MIT](https://choosealicense.com/licenses/mit/)
