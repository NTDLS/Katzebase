/*
drop table #WordIds

create table #WordIds
(
	Id int primary key
)

insert into #WordIds
select Id from word where text in
('cat' ,'van' ,'tennis' ,'chow',
'rooting',
'stifles',
'deltoid',
'vaccine',

'captivation',
'puffs',
'mapped',
'maneuvers',
'stews',
'objectivity'
)

while(1 = 1)
begin--while
	insert into #WordIds
	select distinct TargetWordId from [Synonym]
	where SourceWordId in 
	(select id from #WordIds)
	and TargetWordId not in (select id from #WordIds)

	if @@ROWCOUNT = 0 break;
end--while
*/

select * from [dbo].[FlatSynonyms] where SynonymWordId in (select SourceId from Word where Id in (select Id from #WordIds))
union 
select * from [dbo].[FlatSynonyms] where RootWordId in (select SourceId from Word where Id in (select Id from #WordIds))

select Id,English,German,Spanish,French,Portuguese,Italian,Finnish,Latin,Dutch
from FlatTranslate where Id in (select SourceId from Word where Id in (select Id from #WordIds))

select * from [dbo].[Language] where Id in (select LanguageId from Word where Id in (select Id from #WordIds))
select * from [dbo].[SoundFunction]
select * from [dbo].[SoundMap] where WordId in (select Id from #WordIds)

select * from [dbo].[Synonym] where SourceWordId in (select Id from #WordIds)
union
select * from [dbo].[Synonym] where TargetWordId in (select Id from #WordIds)

select * from [dbo].[Translation] where SourceWordId in (select Id from #WordIds)
union
select * from [dbo].[Translation] where TargetWordId in (select Id from #WordIds)

select Id, Text, LanguageId, SourceId from Word where Id in (select Id from #WordIds)
