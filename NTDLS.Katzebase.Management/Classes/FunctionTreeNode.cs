using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Management.Classes
{
    internal class FunctionTreeNode : TreeNode
    {
        public KbFunctionDescriptor Function { get; set; }

        public FunctionTreeNode(KbFunctionDescriptor function)
        {
            Function = function;
            Text = function.Name;
            ToolTipText = Helpers.Text.SoftWrap(function.Description ?? string.Empty, 65);
        }
    }
}
