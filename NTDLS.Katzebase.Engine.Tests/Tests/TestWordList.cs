using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Tests
{
    public class TestWordList : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public TestWordList(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact(DisplayName = "AggregateWithoutGroupBy")]
        public void TestAggregateWithoutGroupBy()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\AggregateWithoutGroupBy.kbs");

        [Fact(DisplayName = "FieldWhereEqual")]
        public void TestFieldWhereEqual() => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\FieldWhereEqual.kbs");

        [Fact(DisplayName = "GroupAndORDERColumnWhichDoesNotExist")]
        public void TestGroupAndORDERColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\GroupAndORDERColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "SelectColumnWhichDoesNotExist")]
        public void TestSelectColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "SelectColumnWhichDoesNotExistOrExists")]
        public void TestSelectColumnWhichDoesNotExistOrExists()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectColumnWhichDoesNotExistOrExists.kbs");

        [Fact(DisplayName = "SelectGroupByColumnWhichDoesNotExist")]
        public void TestSelectGroupByColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectGroupByColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "SelectGroupByColumnWhichDoesNotExistAgg")]
        public void TestSelectGroupByColumnWhichDoesNotExistAgg()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectGroupByColumnWhichDoesNotExistAgg.kbs");

        [Fact(DisplayName = "SelectSpecificsGroupByAggregateOrderBy")]
        public void TestSelectSpecificsGroupByAggregateOrderBy()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectSpecificsGroupByAggregateOrderBy.kbs");

        [Fact(DisplayName = "SelectStar")]
        public void TestSelectStar()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectStar.kbs");

        [Fact(DisplayName = "SelectStarOrderByColumnWhichDoesNotExist")]
        public void TestSelectStarOrderByColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectStarOrderByColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "SelectStarWhereEqual")]
        public void TestSelectStarWhereEqual()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectStarWhereEqual.kbs");

        [Fact(DisplayName = "SelectTopWithOrderBy")]
        public void TestSelectTopWithOrderBy()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectTopWithOrderBy.kbs");

        [Fact(DisplayName = "SelectTopWithOrderByAndOffset")]
        public void TestSelectTopWithOrderByAndOffset()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectTopWithOrderByAndOffset.kbs");

        [Fact(DisplayName = "SelectWhereColumnWhichDoesNotExist")]
        public void TestSelectWhereColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectWhereColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "WhereEqualSchemaPrefix")]
        public void TestWhereEqualSchemaPrefix()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereEqualSchemaPrefix.kbs");

        [Fact(DisplayName = "WhereEqualSchemaPrefixStar")]
        public void TestWhereEqualSchemaPrefixStar()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereEqualSchemaPrefixStar.kbs");

        [Fact(DisplayName = "WhereLikeNOpen")]
        public void TestWhereLikeNOpen()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereLikeNOpen.kbs");

        [Fact(DisplayName = "WhereLikeOpenN")]
        public void TestWhereLikeOpenN()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereLikeOpenN.kbs");

        [Fact(DisplayName = "WhereNotLikeNOpen")]
        public void TestWhereNotLikeNOpen()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereNotLikeNOpen.kbs");

        [Fact(DisplayName = "WhereNotLikeOpenN")]
        public void TestWhereNotLikeOpenN()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereNotLikeOpenN.kbs");
    }
}
