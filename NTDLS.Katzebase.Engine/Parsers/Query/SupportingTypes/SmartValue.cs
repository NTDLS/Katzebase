using NTDLS.Katzebase.Client.Exceptions;

using static NTDLS.Katzebase.Client.KbConstants;
using fs;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    public class SmartValue
    {
        private fstring? _value = null;

        public KbBasicDataType DataType { get; private set; }

        /// <summary>
        /// This value is a constant string.
        /// </summary>
        public bool IsConstant { get; private set; }

        /// <summary>
        /// This value has been set.
        /// </summary>
        public bool IsSet { get; private set; }

        public fstring? Value => _value;

        public string Prefix { get; private set; } = string.Empty;

        /// <summary>
        /// The schema.field key for the field. Can be parsed to PrefixedField via PrefixedField.Parse(this.Key).
        /// </summary>
        public string Key
            => string.IsNullOrEmpty(Prefix) ? _value?.s ?? "" : $"{Prefix}.{_value}";

        private SmartValue(KbBasicDataType basicDataType)
            => DataType = basicDataType;

        public SmartValue(fstring? value, KbBasicDataType basicDataType)
            => SetValue(value, basicDataType);

        public override string ToString()
            => _value?.ToString() ?? string.Empty;

        public SmartValue Clone()
        {
            return new SmartValue(DataType)
            {
                IsConstant = IsConstant,
                Prefix = Prefix,
                IsSet = IsSet,
                _value = _value
            };
        }

        public void SetValue(fstring? value, KbBasicDataType basicDataType)
        {
            if (basicDataType != KbBasicDataType.Undefined)
            {
                IsConstant = true;
                _value = value?.ToLowerInvariant();
                IsSet = true;
                DataType = basicDataType;
            }
            else
            {
                _value = value?.ToLowerInvariant();

                if (_value != null)
                {
                    if (double.TryParse(_value.s, out _))
                    {
                        throw new KbParserException("Invalid query. Found [" + _value + "], expected identifier.");
                    }
                    else
                    {
                        if (_value.s.Contains('.') && double.TryParse(_value.s, out _) == false)
                        {
                            //schema.field
                            var parts = _value.s.Split('.');
                            if (parts.Length != 2)
                            {
                                throw new KbParserException("Invalid query. Found [" + _value + "], Expected a multi-part condition field.");
                            }

                            Prefix = parts[0];
                            _value = fstring.NewS(parts[1]);
                        }
                        else if (_value != null && _value.s.Contains(':'))
                        {
                            //Check to see if this is a "between" expression "number:number" e.g. 5:10
                            var parts = _value.s.Split(':');
                            if (parts.Length == 2)
                            {
                                if (double.TryParse(parts[0], out _) && double.TryParse(parts[1], out _))
                                {
                                    IsConstant = true;
                                }
                            }
                        }
                        else
                        {
                            _value = value;
                        }
                    }

                    IsSet = true;
                    DataType = basicDataType;
                }
            }
        }
    }
}
