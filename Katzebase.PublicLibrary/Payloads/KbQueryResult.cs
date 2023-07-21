﻿using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return a field-set and the associated row values.
    /// </summary>
    public class KbQueryResult : KbActionResponse
    {
        public List<KbQueryField> Fields { get; set; } = new();
        public List<KbQueryRow> Rows { get; set; } = new();

        public void AddField(string name)
        {
            Fields.Add(new KbQueryField(name));
        }

        public void AddMessage(string text, KbMessageType type)
        {
            Messages.Add(new KbQueryResultMessage(text, type));
        }

        public void AddRow(List<string?> values)
        {
            Rows.Add(new KbQueryRow(values));
        }

        public static KbQueryResult FromActionResponse(KbActionResponse actionResponse)
        {
            return new KbQueryResult()
            {
                RowCount = actionResponse.RowCount,
                Success = actionResponse.Success,
                ExceptionText = actionResponse.ExceptionText,
                Metrics = actionResponse.Metrics,
                Explanation = actionResponse.Explanation,
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
