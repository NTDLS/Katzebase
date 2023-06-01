using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katzebase.Engine.Query
{
    internal class ConditionConsumptionTracker
    {
        public HashSet<Guid> ConsumedSubsets { get; set; } = new();
    }
}
