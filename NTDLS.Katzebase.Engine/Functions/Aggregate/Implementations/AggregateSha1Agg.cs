using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using System.Security.Cryptography;
using System.Text;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    public static class AggregateSha1Agg<TData> where TData : IStringable
    {
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        {
            using var sha1 = SHA1.Create();
            foreach (var str in parameters.AggregationValues.OrderBy(o => o.ToT<string>()))
            {
                var inputBytes = Encoding.UTF8.GetBytes(str.ToT<string>());
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
