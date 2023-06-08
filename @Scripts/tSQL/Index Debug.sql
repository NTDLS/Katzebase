--select count(distinct LocationID), count(distinct Shelf), count(distinct Bin) from [Production].[ProductInventory]

--Key [LocationID:6, Shelf:A, Bin:12] = (3 Documents).

--SELECT * FROM Production.ProductInventory WHERE LocationId = 6 AND Shelf != 'A'-- AND Bin = 12

SELECT
	*
FROM
	Production.ProductInventory
WHERE
	(LocationId = 6 AND Shelf != 'R' AND Quantity = 299)
	OR (LocationId = 6 AND Shelf != 'M' AND Quantity = 299 OR ProductId = 366)
	AND (BIN = 8 OR Bin = 11)
