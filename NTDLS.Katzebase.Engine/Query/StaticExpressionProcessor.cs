using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticExpressionProcessor
    {
        public static void CollapseRow(QueryBatch queryBatch, QueryFieldCollection fieldCollection)
        {
            /* Token Placeholders:
             * 
             * $n_0% = numeric.
             * $s_0% = string.
             * $x_0% = expression (result from a function call).
             * $f_0% = document field placeholder.
             */

            foreach (var field in fieldCollection)
            {
                if (field.Expression is QueryFieldDocumentIdentifier fieldDocumentIdentifier)
                {
                    Console.WriteLine(fieldDocumentIdentifier.Value);

                }
                else if (field.Expression is IQueryFieldExpression fieldExpression)
                {
                    Console.WriteLine(fieldExpression.Value);
                }
                else if (field.Expression is QueryFieldConstantNumeric fieldConstantNumeric)
                {
                    Console.WriteLine(fieldConstantNumeric.Value);
                }
                else if (field.Expression is QueryFieldConstantString fieldConstantString)
                {
                    Console.WriteLine(fieldConstantString.Value);
                }
                else
                {
                    throw new KbParserException($"Unknown expression type: [{field.GetType().Name}]");
                }
            }
        }
    }
}
