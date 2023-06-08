DECLARE @Object nVarChar(500)
DECLARE @Index nVarChar(500)
DECLARE @Column nVarChar(500)
DECLARE @IsUnique bit
DECLARE @IndexId Int
DECLARE @ObjectId Int

declare idxes cursor for
	select
		DB_NAME() + ':' + schema_name(o.schema_id) + ':' + o.name as [Object],
		i.name as [Index],
		i.is_unique as IsUnique,
		o.object_id as ObjectId,
		i.index_id as IndexId
	from
		sys.indexes as i
	inner join sys.objects as o
		on o.object_id = i.object_id
	where
		o.is_ms_shipped = 0
		and o.type = 'u'
		and i.type_desc <> 'HEAP'
	order by
		DB_NAME() + ':' + schema_name(o.schema_id) + ':' + o.name
open idxes

fetch from idxes into @Object, @Index, @IsUnique, @ObjectId, @IndexId

while(@@FETCH_STATUS = 0)
begin

	print 'client.Transaction.Begin();'

	print 'if(client.Schema.Indexes.Exists("' + @Object + '", "' + @Index + '") == false)'
	print '{'
	print 'Console.WriteLine("Creating index: ' + @Object + ' ' + @Index + '");'
	print 'Index index = new Index()'
	print '{'
	print '    Name = "' + @Index + '",'
	print '    IsUnique = ' + case @IsUnique when 0 then 'false' else 'true' end
	print '};'

	----------------------------------------------------------------------------

	declare cols cursor for
		select
			c.name
		from
			sys.index_columns as ic
		inner join sys.columns as c
			on c.object_id = ic.object_id
			and c.column_id = ic.column_id
		where
			ic.object_id = @ObjectId
			and ic.index_id = @IndexId
			and ic.is_included_column = 0
		order by
			ic.index_column_id
	open cols

	fetch from cols into @Column

	while(@@FETCH_STATUS = 0)
	begin

        print 'index.AddAttribute("' + @Column + '");'

		fetch from cols into @Column
	end

	CLOSE cols DEALLOCATE cols
	----------------------------------------------------------------------------

    print 'client.Schema.Indexes.Create("' + @Object + '", index);'
	print '}'
	print 'client.Transaction.Commit();'

	fetch from idxes into @Object, @Index, @IsUnique, @ObjectId, @IndexId
end

CLOSE idxes DEALLOCATE idxes