using NTDLS.Katzebase.Api.Models;
using static NTDLS.Katzebase.Management.Classes.Editor.AutoCompleteFunction;

namespace NTDLS.Katzebase.Management.Classes
{
    internal class FunctionTreeNode : TreeNode
    {
        public KbFunctionDescriptor Function { get; set; }
        public FunctionType FunctionType { get; set; }

        public FunctionTreeNode(KbFunctionDescriptor function, FunctionType functionType)
        {
            Function = function;
            FunctionType = functionType;
            Text = function.Name;
            ToolTipText = Helpers.Text.SoftWrap(function.Description ?? string.Empty, 65);
        }

        /// <summary>
        /// The text that is inserted into the editor when the function is dragged there: system procedures are run with EXEC,
        /// scalar and aggregate functions are used in expressions. The parameters are shown as a comment between the parentheses.
        /// </summary>
        public string GetInsertText()
        {
            var parameters = string.IsNullOrWhiteSpace(Function.Parameters) ? string.Empty : $"/*{Function.Parameters}*/";

            if (FunctionType == FunctionType.System)
            {
                return parameters.Length == 0 ? $"EXEC {Function.Name}" : $"EXEC {Function.Name}({parameters})";
            }

            return $"{Function.Name}({parameters})";
        }
    }
}
