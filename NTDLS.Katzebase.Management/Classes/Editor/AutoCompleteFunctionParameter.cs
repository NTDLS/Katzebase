namespace NTDLS.Katzebase.Management.Classes.Editor
{
    internal class AutoCompleteFunctionParameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }

        public AutoCompleteFunctionParameter(string dataType, string name)
        {
            DataType = dataType;
            Name = name;
        }
    }
}
