namespace NTDLS.Katzebase.Management.Classes.StaticAnalysis
{
    internal class StaticAnalyzer
    {
        private static object _instantiationLock = new();
        private static StaticAnalyzer? _instance;

        public static StaticAnalyzer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instantiationLock)
                    {
                        if (_instance == null)
                        {
                            var instance = new StaticAnalyzer();

                            _instance = instance;
                        }
                    }
                }

                return _instance;
            }
        }

        public StaticAnalyzer()
        {

        }
    }
}
