namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    public class UpsertKeyValues : List<UpsertKeyValue>
    {
        public UpsertKeyValue Add(PrefixedField field, SmartValue value)
        {
            var newValue = new UpsertKeyValue(field, value);
            Add(newValue);
            return newValue;
        }
    }
}
