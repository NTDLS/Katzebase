using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Query
{
    public class SmartValue
    {
        private string? _value = null;

        /// <summary>
        /// This value is a constant string.
        /// </summary>
        public bool IsString { get; private set; }
        /// <summary>
        /// This value is a constant (string or numeric). If false, then this value is a field name.
        /// </summary>
        public bool IsConstant { get; private set; }
        /// <summary>
        /// This value is numeric and does not contain string characters.
        /// </summary>
        public bool IsNumeric { get; private set; }
        /// <summary>
        /// This value has been set.
        /// </summary>
        public bool IsSet { get; private set; }
        public string Prefix { get; private set; } = string.Empty;

        /// <summary>
        /// The schema.field key for the field. Can be parsed to PrefixedField via PrefixedField.Parse(this.Key).
        /// </summary>
        public string Key
            => string.IsNullOrEmpty(Prefix) ? (_value ?? "") : $"{Prefix}.{_value}";

        public SmartValue()
        {
        }

        public SmartValue(string value)
        {
            Value = value;
        }

        public SmartValue Clone()
        {
            return new SmartValue()
            {
                IsConstant = IsConstant,
                IsNumeric = IsNumeric,
                IsString = IsString,
                Prefix = Prefix,
                IsSet = IsSet,
                _value = _value
            };
        }

        public override string ToString()
        {
            return _value?.ToString() ?? string.Empty;
        }

        /*
        public void SetString(string value)
        {
            Value = value;

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
            Value = value;

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
            Value = value;

            IsConstant = true;
            IsNumeric = true;
            IsString = false;

            if (value.All(char.IsDigit) == false)
            {
                throw new KbInvalidArgumentException("Value must be numeric.");
            }
        }
        */

        public string? Value
        {
            get { return _value; }
            set
            {
                IsConstant = false;
                IsNumeric = false;
                IsString = false;

                _value = value?.ToLowerInvariant();

                if (_value == "null")
                {
                    IsConstant = true;
                    _value = null;
                }

                if (_value != null)
                {
                    if (_value.StartsWith('\'') && _value.EndsWith('\''))
                    {
                        //Handle escape sequences:
                        _value = _value.Replace("\\'", "\'");

                        _value = value?.Substring(1, _value.Length - 2);
                        IsString = true;
                        IsConstant = true;
                    }
                    else
                    {
                        if (_value.Contains('.'))
                        {
                            var parts = _value.Split('.');
                            if (parts.Length != 2)
                            {
                                throw new KbParserException("Invalid query. Found [" + _value + "], Expected a multi-part condition field.");
                            }

                            Prefix = parts[0];
                            _value = parts[1];
                        }
                    }

                    if (_value != null && _value.All(char.IsDigit))
                    {
                        IsConstant = true;
                        IsNumeric = true;
                    }
                }

                IsSet = true;
            }
        }
    }
}
