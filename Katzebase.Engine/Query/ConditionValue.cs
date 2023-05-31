namespace Katzebase.Engine.Query
{
    public class ConditionValue
    {
        public bool IsString { get; set; }
        public bool IsConstant { get; set; }
        public bool IsNumeric { get; set; }

        private string? _value = null;
        public bool IsSet { get; private set; }

        public override string ToString()
        {
            return _value?.ToString() ?? string.Empty;
        }

        public string? Value
        {
            get { return _value; }
            set
            {
                IsConstant = false;
                IsNumeric = false;
                IsString = false;

                _value = value?.ToLowerInvariant();

                if (_value != null)
                {
                    //Hanlde escape sequences:
                    _value = _value.Replace("\\'", "\'");

                    if (_value.StartsWith('\'') && _value.EndsWith('\''))
                    {
                        _value = _value.Substring(1, _value.Length - 2);
                        IsString = true;
                        IsConstant = true;
                    }

                    if (_value.All(Char.IsDigit))
                    {
                        IsConstant = true;
                        IsNumeric = true;
                    }
                    IsSet = true;
                }
            }
        }
    }
}
