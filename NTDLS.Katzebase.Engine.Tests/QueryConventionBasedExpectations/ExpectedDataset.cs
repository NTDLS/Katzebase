using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using System.Diagnostics.CodeAnalysis;
using static NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations.Constants;

namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    internal class ExpectedDataset
    {
        public List<string> Fields { get; set; } = new();
        public List<ExpectedRow> Rows { get; set; } = new();
        public KbInsensitiveDictionary<FieldPattern> FieldPatterns { get; set; } = new();
        public KbInsensitiveDictionary<QueryAttribute> Options { get; set; } = new();

        /// <summary>
        /// Returns the integer index of the field name, throws exception if not found.
        /// </summary>
        public int FieldIndex(string fieldName)
        {
            if (Fields != null)
            {
                int index = 0;
                foreach (var field in Fields)
                {
                    if (field.Is(fieldName))
                    {
                        return index;
                    }
                    index++;
                }
            }
            throw new Exception($"Field {fieldName} was not found in the collection.");
        }

        #region Get Options.

        public bool IsOptionSet(DatasetExpectationOption ops)
            => Options.TryGetValue(ops.ToString(), out var _);

        public bool TryGetOption<T>(DatasetExpectationOption opt, out T outValue, T defaultValue)
        {
            if (Options.TryGetValue(opt.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = defaultValue;
            return false;
        }

        public bool TryGetOption<T>(DatasetExpectationOption opt, [NotNullWhen(true)] out T? outValue)
        {
            if (Options.TryGetValue(opt.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = default;
            return false;
        }

        public T GetOption<T>(DatasetExpectationOption opt, T defaultValue)
        {
            if (Options.TryGetValue(opt.ToString(), out var option))
            {
                return (T)option.Value;
            }
            return defaultValue;
        }

        #endregion
    }
}
