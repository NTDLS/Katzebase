using System.Collections.Generic;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class Conditions
    {
        public List<Condition> Collection = new List<Condition>();

        private List<Conditions> _Nest = new List<Conditions>();

        public List<Conditions> Nest
        {
            get
            {
                if (_Nest == null)
                {
                    _Nest = new List<Conditions>();
                }
                return _Nest;
            }
            set
            {
                _Nest = value;
            }
        }

        public ConditionType ConditionType { get; set; }

        public bool LowerCased { get; set; }

        public void MakeLowerCase()
        {
            if (LowerCased == false)
            {
                LowerCased = true;
                foreach (Condition condition in Collection)
                {
                    condition.Key = condition.Key.ToLower();
                    condition.Value = condition.Value.ToLower();
                }

                if (_Nest != null)
                {
                    foreach (Conditions nestedCOnditions in _Nest)
                    {
                        nestedCOnditions.MakeLowerCase();
                    }
                }
            }
        }

        public Conditions()
        {
        }

        public void Add(Conditions conditions)
        {
            this.Nest = conditions.Nest;
            foreach (Condition condition in conditions.Collection)
            {
                this.Add(condition);
            }
        }

        public void Add(ConditionType conditionType, string key, ConditionQualifier conditionQualifier, string value)
        {
            this.Collection.Add(new Condition(conditionType, key.ToLower(), conditionQualifier, value.ToLower()));
        }

        public void Add(Condition condition)
        {
            this.Add(condition.ConditionType, condition.Key, condition.ConditionQualifier, condition.Value);
        }
    }
}
