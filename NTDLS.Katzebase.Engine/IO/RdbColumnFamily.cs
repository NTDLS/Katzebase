using RocksDbSharp;

namespace NTDLS.Katzebase.Engine.IO
{
    public class RdbColumnFamily
    {
        public string Name { get; private set; }
        public ColumnFamilyHandle Handle { get; private set; }

        public RdbColumnFamily(string name, ColumnFamilyHandle handle)
        {
            Name = name;
            Handle = handle;
        }
    }
}
