namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    internal class Constants
    {
        public enum DatasetExpectationOption
        {
            EnforceRowOrder,
            HasFieldNames,
            AffectedCount,
            MaxDuration
        }

        public enum BatchExpectationOption
        {
            DoNotValidate
        }
    }
}
