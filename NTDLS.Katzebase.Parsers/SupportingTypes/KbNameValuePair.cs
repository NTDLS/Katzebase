﻿namespace NTDLS.Katzebase.Parsers.SupportingTypes
{
    public class KbNameValuePair<TKey, TValue>(TKey name, TValue value)
    {
        public TKey Name { get; set; } = name;
        public TValue Value { get; set; } = value;
    }
}
