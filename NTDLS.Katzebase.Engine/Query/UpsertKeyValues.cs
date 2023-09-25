namespace Katzebase.Engine.Query
{
    public class UpsertKeyValues : List<UpsertKeyValue>
    {
        public UpsertKeyValue Add(PrefixedField field, SmartValue value)
        {
            var newValue = new UpsertKeyValue(field, value);
            base.Add(newValue);
            return newValue;
        }
    }
}
