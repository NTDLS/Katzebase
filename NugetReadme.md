# Katzebase
Katzebase is a document-based database written in .net 7 that runs on Windows or Linux. By default It can run as a service but the libraries can also be embedded. It basically supports what you’d expect from a typical relational-database-management-system except the "rows" are stored as json and the schema is not fixed. The engine is wrapped by MVC controllers and allows access via APIs, a SQL like syntax or by using the bundled management UI (which calls the APIs).

Check out the full documentation at https://katzebase.com/

## Features:
- Abortable transactions.
- Caching and write deferment.
- Locking, isolation and atomicity.
- Document indexing.
- Partitioning.
- Logging and health monitoring.
- SQL Query language with support for (field list, joins, top(count), where clause).

## Sample Data
To run the included examples, download the [sample Katzebase database]( https://katzebase.com/Download/Katzebase.zip).

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. If you want to join the project, just email me (its on my profile).

## License
[Apache-2.0](https://choosealicense.com/licenses/apache-2.0/)
