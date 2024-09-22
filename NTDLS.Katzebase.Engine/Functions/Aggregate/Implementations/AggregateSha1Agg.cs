using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using System.Security.Cryptography;
using System.Text;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateSha1Agg
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            using var sha1 = SHA1.Create();
            foreach (var str in parameters.AggregationValues.OrderBy(o => o))
            {
                var inputBytes = Encoding.UTF8.GetBytes(str);
                sha1.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
            }

            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            var sb = new StringBuilder();
            var bytes = sha1.Hash ?? Array.Empty<byte>();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

    }
}
