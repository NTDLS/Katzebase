using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Functions.System;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    internal class MockEngineCore
    {
        private static readonly object _instantiationLock = new();
        private static MockEngineCore? _instance;

        public static MockEngineCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instantiationLock)
                    {
                        if (_instance == null)
                        {
                            var instance = new MockEngineCore();

                            _instance = instance;
                        }
                    }
                }

                return _instance;
            }
        }

        public KbInsensitiveDictionary<KbVariable> GlobalTokenizerConstants { get; } = new();

        public MockEngineCore()
        {
            //Define all query literal constants here, these will be filled in my the tokenizer. Do not use quotes for strings.
            GlobalTokenizerConstants.Add("true", new("1", KbBasicDataType.Numeric));
            GlobalTokenizerConstants.Add("false", new("0", KbBasicDataType.Numeric));
            GlobalTokenizerConstants.Add("null", new(null, KbBasicDataType.Undefined));

            SystemFunctionCollection.Initialize();
            ScalarFunctionCollection.Initialize();
            AggregateFunctionCollection.Initialize();
        }
    }
}
