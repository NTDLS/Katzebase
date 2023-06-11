# Katzebase
![Catzebase](https://user-images.githubusercontent.com/11428567/226699701-9111e5d9-9555-43d3-a55e-4f5d287cadf3.png)

Katzebase is a fully ACID compliant document-based data storage engine that mimics a relational database. The engine is wrapped by MVC controllers and allows access via APIs, a SQL like syntax or by using the bundled management UI.

## Features:
- Abortable transactions.
- Caching and write deferment.
- Locking, isolation and atomicity.
- Document indexing.
- Logging and health monitoring.
- SQL Query language with support for:
  - Field list.
  - INNER JOIN.
  - TOP(count).
  - WHERE clause.

## Sample Data
You can download the [sample Katzebase database](https://networkdls.com/DirectDownload/Katzebase/AdventureWorks.7z), which is a dump of the AdventureWorks2012 SQL Server database. Yea, I know its old - but thats the database I had on hand.

## Contributing

Pull requests are welcome. For major changes, please open an issue first
to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License

[MIT](https://choosealicense.com/licenses/mit/)
