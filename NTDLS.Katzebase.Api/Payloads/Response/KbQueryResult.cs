using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Api.Payloads.Response
{
    /// <summary>
    /// KbQueryResult is used to return a field-set and the associated row values.
    /// </summary>
    public class KbQueryResult : KbBaseActionResponse
    {
        public List<KbQueryField> Fields { get; set; } = new();
        public List<KbQueryRow> Rows { get; set; } = new();

        public void AddField(string name)
        {
            Fields.Add(new KbQueryField(name));
        }

        /// <summary>
        /// Returns the integer index of the field name, throws exception if not found.
        /// </summary>
        public int IndexOf(string fieldName)
        {
            int index = 0;
            foreach (var field in Fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return index;
                }
                index++;
            }

            throw new Exception($"Field {fieldName} was not found in the collection.");
        }

        /// <summary>
        /// Returns the value of the given row and field.
        /// </summary>
        public string? RowValue(int rowIndex, int fieldIndex)
        {
            return Rows[rowIndex].Values[fieldIndex];
        }

        /// <summary>
        /// Returns the value of the given field on the given row.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string? RowValue(KbQueryRow row, string fieldName)
        {
            int fieldIndex = IndexOf(fieldName);
            return row.Values[fieldIndex];
        }

        /// <summary>
        /// Returns the value of the given field index on the given row.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string? RowValue(KbQueryRow row, int fieldIndex)
        {
            return row.Values[fieldIndex];
        }

        public void AddRow(List<string?> values)
        {
            Rows.Add(new KbQueryRow(values));
        }

        public static KbQueryResult FromActionResponse(KbBaseActionResponse actionResponse)
        {
            return new KbQueryResult()
            {
                Messages = actionResponse.Messages,
                RowCount = actionResponse.RowCount,
                Metrics = actionResponse.Metrics,
                Duration = actionResponse.Duration,
            };
        }

        public KbQueryResultCollection ToCollection()
        {
            var result = new KbQueryResultCollection();
            result.Add(this);
            return result;
        }
    }
}
