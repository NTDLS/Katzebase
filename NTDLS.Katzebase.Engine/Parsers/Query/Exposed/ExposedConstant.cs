﻿using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Exposed
{
    /// <summary>
    /// The "exposed" classes are helpers that allow us to access the ordinal of fields as well as the some of the nester properties.
    /// This one is for fields that are constants.
    /// </summary>
    internal class ExposedConstant
    {
        public int Ordinal { get; private set; }
        public string FieldAlias { get; private set; }
        public string Value { get; private set; }
        public BasicDataType DataType { get; private set; }

        public ExposedConstant(int ordinal, BasicDataType dataType, string alias, string value)
        {
            Ordinal = ordinal;
            DataType = dataType;
            FieldAlias = alias;
            Value = value;
        }
    }
}