select
	sw.Id,
	sw.Text as SourceWord,
	S.TargetWordId,
	tw.Text as TargetWord,
	tl.Name as TargetLanguage,
	tw.Id
from
	Word as sw
inner join Synonym as S
	on S.SourceWordId = sw.Id
inner join Word as tw
	on tw.Id = S.TargetWordId
inner join Language as tl
	ON tl.Id = tw.LanguageId
inner join Language as sl
	ON sl.Id = sw.LanguageId
where
	sw.Text = 'moon'
	and sl.Name = 'English'
