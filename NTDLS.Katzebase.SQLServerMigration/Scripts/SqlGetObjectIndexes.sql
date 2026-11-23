SELECT 
    i.name AS IndexName,
    case when i.is_unique = 1 OR i.is_unique_constraint = 1 THEN 1 else 0 end IsUnique,
    c.name AS ColumnName 
FROM 
    sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE 
    i.is_disabled = 0  -- Exclude disabled indexes
 AND ic.is_included_column = 0
 AND '[' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + ']' = @ObjectName
ORDER BY 
    t.name, i.name, ic.key_ordinal
