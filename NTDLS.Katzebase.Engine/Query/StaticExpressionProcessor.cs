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
             * $n_0% = numeric
             * $s_0% = string
             * $p_0% = parameter (user variable)
             * $x_0% = expression (result from a function call).
             * 
            */

            foreach (var field in fieldCollection)
            {
                if (field.Expression is QueryFieldDocumentIdentifier fieldDocumentIdentifier)
                {
                    Console.WriteLine(fieldDocumentIdentifier.QualifiedField);

                }
                else if (field.Expression is IQueryFieldExpression fieldExpression)
                {
                    Console.WriteLine(fieldExpression.Expression);
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
