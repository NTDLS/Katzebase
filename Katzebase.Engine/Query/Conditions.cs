using Katzebase.Engine.Documents;
using Katzebase.Library;
using Katzebase.Library.Client.Management;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class Conditions
    {
        public List<ConditionGroup> Groups { get; set; } = new();
        public LogicalConnector LogicalConnector { get; set; }

        /*
        public bool LowerCased { get; private set; } = false;

        public void MakeLowerCase(bool force = false)
        {
            if (LowerCased == false || force)
            {
                LowerCased = true;
                foreach (Condition condition in Collection)
                {
                    condition.Field = condition.Field.ToLower();
                    condition.Value = condition.Value.ToLower();
                }

                if (_groups != null)
                {
                    foreach (Conditions nestedConditions in _groups)
                    {
                        nestedConditions.MakeLowerCase();
                    }
                }
            }
        }
        */

        public Conditions()
        {
        }

        /*
        public void AddRange(Conditions conditions)
        {
            Groups = conditions.Groups;
            foreach (Condition condition in conditions.Collection)
            {
                Add(condition);
            }
        }

        public Condition Add(Condition condition)
        {
            var result = new Condition(condition.LogicalConnector, condition.Field, condition.ConditionQualifier, condition.Value);
            result.Children.AddRange(condition.Children);
            Collection.Add(result);
            return result;
        }

        public bool IsMatch(PersistDocument persistDocument)
        {
            Utility.EnsureNotNull(persistDocument);
            Utility.EnsureNotNull(persistDocument.Content);

            JObject jsonContent = JObject.Parse(persistDocument.Content);

            return IsMatch(jsonContent);
        }

        public bool IsMatch(JObject jsonContent)
        {
            bool fullAttributeMatch = true;

            //Loop though each condition in the prepared query:
            foreach (var condition in Collection)
            {
                //Get the value of the condition:
                if (jsonContent.TryGetValue(condition.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                {
                    //If the condition does not match the value in the document then we break from checking the remainder of the conditions for this document and continue with the next document.
                    //Otherwise we continue to the next condition until all conditions are matched.
                    if (condition.IsMatch(jToken.ToString().ToLower()) == false)
                    {
                        fullAttributeMatch = false;
                        break;
                    }
                }
            }

            return fullAttributeMatch;
        }
        */
    }
}
