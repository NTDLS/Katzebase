﻿namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    public enum KbAggregateFunctionParameterType
    {
        Undefined,
        String,
        Boolean,
        Numeric,
        StringInfinite,
        NumericInfinite,
        AggregationArray //The first parameter of all aggregation functions.
    }
}