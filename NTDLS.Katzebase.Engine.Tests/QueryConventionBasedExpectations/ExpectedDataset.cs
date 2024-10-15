using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics.CodeAnalysis;
using static NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations.Constants;

namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    internal class ExpectedDataset
    {
        public List<string>? Fields { get; set; }
        public List<ExpectedRow> Rows { get; set; } = new();
        public KbInsensitiveDictionary<QueryAttribute> Options { get; set; } = new();

        #region Get Attributes.

        public bool IsAttributeSet(ExpectationAttribute attribute)
            => Options.TryGetValue(attribute.ToString(), out var _);

        public bool TryGetAttribute<T>(ExpectationAttribute attribute, out T outValue, T defaultValue)
        {
            if (Options.TryGetValue(attribute.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = defaultValue;
            return false;
        }

        public bool TryGetAttribute<T>(ExpectationAttribute attribute, [NotNullWhen(true)] out T? outValue)
        {
            if (Options.TryGetValue(attribute.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = default;
            return false;
        }

        public T GetAttribute<T>(ExpectationAttribute attribute, T defaultValue)
        {
            if (Options.TryGetValue(attribute.ToString(), out var option))
            {
                return (T)option.Value;
            }
            return defaultValue;
        }

        #endregion
    }
}
