using System.Collections.Generic;
using Katzebase.Engine.Query;

namespace Katzebase.Engine.Indexes
{
    public class IndexKeyMatches : List<IndexKeyMatch>
    {
        public IndexKeyMatches(Conditions conditions)
        {
            conditions.MakeLowerCase();

            foreach (Condition condition in conditions.Collection)
            {
                this.Add(new IndexKeyMatch(condition));
            }
        }

        public IndexKeyMatches()
        {
        }
    }
}
