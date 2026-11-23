SELECT 
    '[' + OBJECT_SCHEMA_NAME(SysT.[object_id]) + '].[' + SysT.[name] +']' AS [SourceSchemaObject],
    OBJECT_SCHEMA_NAME(SysT.[object_id]) + ':' + SysT.[name] AS [TargetSchemaObject], 
    SysT.[name] AS [TargetObject],
    ISNULL(SUM(p.reserved_page_count) * 8 * 1024, 0) AS TotalSizeBytes,  -- Total size in bytes
    ISNULL(SUM(p.row_count), 0) AS TotalRows,                            -- Total rows
    CASE 
        WHEN SUM(p.row_count) > 0 THEN (SUM(p.reserved_page_count) * 8 * 1024) / SUM(p.row_count) 
        ELSE 0 
    END AS AvgRowSizeBytes                                               -- Avg row size in bytes
FROM 
    sys.tables AS SysT
LEFT JOIN 
    sys.dm_db_partition_stats AS p ON p.object_id = SysT.object_id AND p.index_id IN (0, 1)  -- Include only heap (0) and clustered index (1)
WHERE 
    SysT.type = 'u'  -- Only user tables
GROUP BY 
    SysT.[object_id], SysT.[name], OBJECT_SCHEMA_NAME(SysT.[object_id])
ORDER BY 
    OBJECT_SCHEMA_NAME(SysT.[object_id]) + '.' + SysT.[name];
