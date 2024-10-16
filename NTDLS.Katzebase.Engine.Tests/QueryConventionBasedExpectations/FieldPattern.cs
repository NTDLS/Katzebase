using static NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations.Constants;

namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    internal class FieldPattern
    {
        public FieldPatternType PatternType { get; set; }
        public string Pattern { get; set; }

        public FieldPattern(FieldPatternType patternType, string pattern)
        {
            PatternType = patternType;
            Pattern = pattern;
        }
    }
}
