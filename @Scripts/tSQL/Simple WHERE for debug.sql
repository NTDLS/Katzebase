SELECT
	*
FROM
	Production.ProductInventory
WHERE
	(
		LocationId = 6
		AND Shelf != 'R'
		AND Quantity = 299
	)
	OR (
		(
			LocationId = 6 AND Shelf != 'M'
		)
		AND Quantity = 299 OR ProductId = 366
	)
	AND
	(
		BIN = 8
		OR Bin = 11
	)
