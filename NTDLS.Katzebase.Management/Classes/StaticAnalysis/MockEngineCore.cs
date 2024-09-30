using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Functions.System;
using NTDLS.Katzebase.Parsers.Interfaces;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Management.Classes.StaticAnalysis
{
    internal class MockEngineCore : IEngineCore
    {
        private static object _instantiationLock = new();
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

        public KbInsensitiveDictionary<KbConstant> GlobalConstants { get; } = new();

        public MockEngineCore()
        {
            //Define all query literal constants here, these will be filled in my the tokenizer. Do not use quotes for strings.
            GlobalConstants.Add("true", new("1", KbBasicDataType.Numeric));
            GlobalConstants.Add("false", new("0", KbBasicDataType.Numeric));
            GlobalConstants.Add("null", new(null, KbBasicDataType.Undefined));

            SystemFunctionCollection.Initialize();
            ScalerFunctionCollection.Initialize();
            AggregateFunctionCollection.Initialize();
        }
    }
}
