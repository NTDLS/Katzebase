using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
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
            => string.IsNullOrEmpty(Prefix) ? _value ?? "" : $"{Prefix}.{_value}";

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
                        if (_value.Contains('.') && double.TryParse(_value, out _) == false)
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

                    if (_value != null && double.TryParse(_value, out _))
                    {
                        IsConstant = true;
                        IsNumeric = true;
                    }
                    else if (_value != null && _value.Contains(':'))
                    {
                        //Check to see if this is a "between" expression "number:number" e.g. 5:10
                        var parts = _value.Split(':');
                        if (parts.Length == 2)
                        {
                            if (double.TryParse(parts[0], out _) && double.TryParse(parts[1], out _))
                            {
                                IsConstant = true;
                            }
                        }
                    }
                }

                IsSet = true;
            }
        }
    }
}
