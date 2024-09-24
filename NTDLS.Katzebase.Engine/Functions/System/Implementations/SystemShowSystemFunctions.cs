using fs;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using System.Text;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowSystemFunctions
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("Name");
            result.AddField("Parameters");

            foreach (var prototype in SystemFunctionCollection.Prototypes)
            {
                var parameters = new StringBuilder();

                foreach (var param in prototype.Parameters)
                {
                    parameters.Append($"{param.Name}:{param.Type}");
                    if (param.HasDefault)
                    {
                        parameters.Append($" = {param.DefaultValue}");
                    }
                    parameters.Append(", ");
                }
                if (parameters.Length > 2)
                {
                    parameters.Length -= 2;
                }

                var values = new List<fstring?>
                {
                    fstring.NewS(prototype.Name),
                    fstring.NewS(parameters.ToString())
                };
                result.AddRow(values);

#if DEBUG
                //This is to provide code for the documentation wiki.
                var wikiPrototype = new StringBuilder();

                wikiPrototype.Append($" ##Color(#c6680e, {prototype.Name})(");

                if (prototype.Parameters.Count > 0)
                {
                    for (int i = 0; i < prototype.Parameters.Count; i++)
                    {
                        var param = prototype.Parameters[i];

                        wikiPrototype.Append($"##Color(#318000, {param.Type}) ##Color(#c6680e, {param.Name})");
                        if (param.HasDefault)
                        {
                            wikiPrototype.Append($" = ##Color(#CC0000, \"'{param.DefaultValue}'\")");
                        }
                        wikiPrototype.Append(", ");
                    }
                    if (wikiPrototype.Length > 2)
                    {
                        wikiPrototype.Length -= 2;
                    }
                }
                wikiPrototype.Append(')');
                result.Messages.Add(new KbQueryResultMessage(wikiPrototype.ToString(), KbConstants.KbMessageType.Verbose));
#endif
            }

            return collection;
        }
    }
}
