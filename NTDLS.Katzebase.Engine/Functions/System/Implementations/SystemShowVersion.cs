using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowVersion<TData> where TData : IStringable
    {
        public static KbQueryResultCollection<TData> Execute(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function)
        {
            var showAll = function.Get("showAll", false);

            var collection = new KbQueryResultCollection<TData>();
            var result = collection.AddNew();

            result.AddField("Assembly");
            result.AddField("Version");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(o => o.FullName))
            {
                try
                {
                    var assemblyName = assembly.GetName();

                    if (string.IsNullOrEmpty(assembly.Location))
                    {
                        continue;
                    }

                    if (showAll == false)
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                        string? companyName = versionInfo.CompanyName;

                        if (companyName?.ToLower()?.Contains("networkdls") != true)
                        {
                            continue;
                        }
                    }

                    var values = new List<TData>(new[]
                    {
                        $"{assemblyName.Name}",
                        $"{assemblyName.Version}"
                    }.Select(s => s.CastToT<TData>(EngineCore<TData>.StrCast)));
                    result.AddRow(values);
                }
                catch
                {
                }
            }
            return collection;
        }
    }
}
