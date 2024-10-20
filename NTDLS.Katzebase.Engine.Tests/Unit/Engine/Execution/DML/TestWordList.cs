using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestWordList(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        [Fact(DisplayName = "Select Aggregate without GroupBY")]
        public void TestAggregateWithoutGroupBy()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\AggregateWithoutGroupBy.kbs");

        [Fact(DisplayName = "Select Where field equals")]
        public void TestFieldWhereEqual() => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\FieldWhereEqual.kbs");

        [Fact(DisplayName = "Select GroupBy and OrderBy column which does not exist")]
        public void TestGroupAndORDERColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\GroupAndORDERColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "Select Where column does not exist")]
        public void TestSelectColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "Select Where column does not exist or which does exist")]
        public void TestSelectColumnWhichDoesNotExistOrExists()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectColumnWhichDoesNotExistOrExists.kbs");

        [Fact(DisplayName = "Select GroupBy column does not exist")]
        public void TestSelectGroupByColumnWhichDoesNotExistAgg()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectGroupByColumnWhichDoesNotExistAgg.kbs");

        [Fact(DisplayName = "Select from Joins with GroupBy and OrderBy")]
        public void TestSelectSpecificsGroupByAggregateOrderBy()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectSpecificsGroupByAggregateOrderBy.kbs");

        [Fact(DisplayName = "Select *")]
        public void TestSelectStar()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectStar.kbs");

        [Fact(DisplayName = "Select * where column does not exist")]
        public void TestSelectStarOrderByColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectStarOrderByColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "Select * Where equal")]
        public void TestSelectStarWhereEqual()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectStarWhereEqual.kbs");

        [Fact(DisplayName = "Select Top with OrderBy")]
        public void TestSelectTopWithOrderBy()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectTopWithOrderBy.kbs");

        [Fact(DisplayName = "Select Top with Order By and Offset")]
        public void TestSelectTopWithOrderByAndOffset()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectTopWithOrderByAndOffset.kbs");

        [Fact(DisplayName = "Select where column does not exist")]
        public void TestSelectWhereColumnWhichDoesNotExist()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\SelectWhereColumnWhichDoesNotExist.kbs");

        [Fact(DisplayName = "Select * where schema.field")]
        public void TestWhereEqualSchemaPrefix()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereEqualSchemaPrefix.kbs");

        [Fact(DisplayName = "Select schema.* where schema.field")]
        public void TestWhereEqualSchemaPrefixStar()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereEqualSchemaPrefixStar.kbs");

        [Fact(DisplayName = "Where Like ...%")]
        public void TestWhereLikeNOpen()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereLikeNOpen.kbs");

        [Fact(DisplayName = "Where Not Like %...")]
        public void TestWhereLikeOpenN()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereLikeOpenN.kbs");

        [Fact(DisplayName = "Where Not Like ...%")]
        public void TestWhereNotLikeNOpen()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereNotLikeNOpen.kbs");

        [Fact(DisplayName = "Where Not Like %...")]
        public void TestWhereNotLikeOpenN()
            => QueryExpectation.ValidateScriptResults(_engine, @"TestCases\WordList\WhereNotLikeOpenN.kbs");
    }
}
