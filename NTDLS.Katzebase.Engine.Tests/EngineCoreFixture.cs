namespace NTDLS.Katzebase.Engine.Tests
{
    public class EngineCoreFixture : IDisposable
    {
        public EngineCore Engine { get; private set; }

        public EngineCoreFixture()
        {
            Engine = EngineCoreSingleton.GetSingleInstance();
        }

        public void Dispose()
        {
            EngineCoreSingleton.Dereference();
        }
    }
}
