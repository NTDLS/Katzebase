using Katzebase.Library.Exceptions;

namespace Katzebase.Engine.Query
{
    public class ConditionValue
    {
        /// <summary>
        /// This value is a constant string.
        /// </summary>
        public bool IsString { get; set; }
        /// <summary>
        /// This value is a constant (string or numeric). If false, then this value is a field name.
        /// </summary>
        public bool IsConstant { get; set; }
        /// <summary>
        /// This value is numeric and does not contain string characters.
        /// </summary>
        public bool IsNumeric { get; set; }
        /// <summary>
        /// This value has been set.
        /// </summary>
        public bool IsSet { get; private set; }

        private string? _value = null;

        public ConditionValue Clone()
        {
            return new ConditionValue()
            {
                IsConstant = IsConstant,
                IsNumeric = IsNumeric,
                IsString = IsString,
                IsSet = IsSet,
                _value = _value
            };
        }

        public override string ToString()
        {
            return _value?.ToString() ?? string.Empty;
        }

        public void SetString(string value)
        {
            this.Value = value;

            IsConstant = true;
            IsNumeric = false;
            IsString = true;
        }

        /// <summary>
        /// This is a field name, not a string.
        /// </summary>
        /// <param name="value"></param>
        public void SetField(string value)
        {
            this.Value = value;

            IsConstant = false;
            IsNumeric = false;
            IsString = false;
        }

        /// <summary>
        /// This is a constant number.
        /// </summary>
        /// <param name="value"></param>
        public void SetNumeric(string value)
        {
            this.Value = value;

            IsConstant = true;
            IsNumeric = true;
            IsString = false;

            if (value.All(Char.IsDigit) == false)
            {
                throw new KbInvalidArgumentException("The value must be numeric.");
            }
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
                    //Handle escape sequences:
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
