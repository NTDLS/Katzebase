using System.Collections.Generic;

namespace Katzebase.Engine.Query
{
    public class UpsertKeyValues
    {
        public List<UpsertKeyValue> Collection { get; set; }

        public UpsertKeyValues()
        {
            Collection = new List<UpsertKeyValue>();
        }

        public bool LowerCased { get; set; }

        public void MakeLowerCase()
        {
            if (LowerCased == false)
            {
                LowerCased = true;
                foreach (UpsertKeyValue kvp in Collection)
                {
                    kvp.Key = kvp.Key.ToLower();
                }
            }
        }
    }
}
