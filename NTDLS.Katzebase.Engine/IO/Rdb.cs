using NTDLS.Katzebase.Shared;
using RocksDbSharp;
using System.Collections.Concurrent;
using System.Text;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.IO
{
    public class Rdb
        : IDisposable
    {
        private bool disposedValue;

        public RocksDb Instance { get; private set; }
        public ConcurrentDictionary<string, RdbColumnFamily> ColumnFamilies { get; private set; } = new();
        public string Path { get; private set; }

        public Rdb(string path)
        {
            Path = path;
            var options = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);
            var cfOptions = new ColumnFamilyOptions();

            var columnFamilies = new ColumnFamilies();
            foreach (var cf in RocksDb.ListColumnFamilies(options, path))
                columnFamilies.Add(cf, cfOptions);

            Instance = RocksDb.Open(options, path, columnFamilies);
        }

        public Iterator NewIterator(RdbColumnFamily columnFamily)
        {
            return Instance.NewIterator(columnFamily.Handle);
        }

        public Iterator NewIterator(KbColumnFamilyName columnFamilyName)
        {
            var columnFamily = GetColumnFamily(columnFamilyName);
            return Instance.NewIterator(columnFamily.Handle);
        }

        public void DropColumnFamily(KbColumnFamilyName name)
        {
            // Load the handle into the cache if not already present, then remove and dispose it.
            // RocksDB requires all handles to a CF to be destroyed before the drop is durably
            // recorded in the MANIFEST (survives restart). Disposing the handle achieves this.
            var cf = GetColumnFamily(name);
            ColumnFamilies.TryRemove(name.ToString(), out _);
            Instance.DropColumnFamily(cf.Handle);
            cf.Handle.Dispose();
        }

        public void DropColumnFamily(RdbKey key)
        {
            var cf = GetColumnFamily(key);
            ColumnFamilies.TryRemove(key.ToString(), out _);
            Instance.DropColumnFamily(cf.Handle);
            cf.Handle.Dispose();
        }

        #region CreateColumnFamily

        public RdbColumnFamily CreateColumnFamily(KbColumnFamilyName name)
            => CreateColumnFamily(name.ToString());

        public RdbColumnFamily CreateColumnFamily(RdbKey key)
            => CreateColumnFamily(key.ToString());

        public RdbColumnFamily CreateColumnFamily(string name)
        {
            return ColumnFamilies.GetOrAdd(name, n =>
            {
                return new RdbColumnFamily(n, Instance.CreateColumnFamily(new ColumnFamilyOptions(), name));
            });
        }

        #endregion

        #region GetColumnFamily

        public RdbColumnFamily GetColumnFamily(KbColumnFamilyName name)
            => GetColumnFamily(name.ToString());

        public RdbColumnFamily GetColumnFamily(RdbKey key)
            => GetColumnFamily(key.ToString());

        public RdbColumnFamily GetColumnFamily(string name)
        {
            return ColumnFamilies.GetOrAdd(name, n =>
            {
                try { return new RdbColumnFamily(n, Instance.GetColumnFamily(name)); }
                catch { return new RdbColumnFamily(n, Instance.CreateColumnFamily(new ColumnFamilyOptions(), name)); }
            });
        }

        #endregion

        public byte[] Get(byte[] key, KbColumnFamilyName cfName) => Get(key, GetColumnFamily(cfName));
        public void Put(byte[] key, byte[]? value, KbColumnFamilyName cfName) => Put(key, value, GetColumnFamily(cfName));
        public void Remove(byte[] key, KbColumnFamilyName cfName) => Remove(key, GetColumnFamily(cfName));

        public string Get(string key, RdbColumnFamily? cf = null, ReadOptions? readOptions = null, Encoding? encoding = null)
            => Instance.Get(key, cf?.Handle, readOptions, encoding);

        public byte[] Get(byte[] key, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, cf?.Handle, readOptions);

        public byte[] Get(ReadOnlySpan<byte> key, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, cf?.Handle, readOptions);

        public bool GetFixedSizeValue(ReadOnlySpan<byte> key, Span<byte> fixedSizeValueOutput, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.GetFixedSizeValue(key, fixedSizeValueOutput, cf?.Handle, readOptions);

        public bool HasKey(ReadOnlySpan<byte> key, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.HasKey(key, cf?.Handle, readOptions);

        public T Get<T>(ReadOnlySpan<byte> key, ISpanDeserializer<T> deserializer, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, deserializer, cf?.Handle, readOptions);

        public T Get<T>(ReadOnlySpan<byte> key, Func<Stream, T> deserializer, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, deserializer, cf?.Handle, readOptions);

        public byte[] Get(byte[] key, long keyLength, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, keyLength, cf?.Handle, readOptions);

        public bool HasKey(byte[] key, long keyLength, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.HasKey(key, keyLength, cf?.Handle, readOptions);

        public bool HasKey(string key, RdbColumnFamily? cf = null, ReadOptions? readOptions = null, Encoding? encoding = null)
            => Instance.HasKey(key, cf?.Handle, readOptions, encoding);

        public long Get(byte[] key, byte[] buffer, long offset, long length, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, buffer, offset, length, cf?.Handle, readOptions);

        public long Get(byte[] key, long keyLength, byte[] buffer, long offset, long length, RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.Get(key, keyLength, buffer, offset, length, cf?.Handle, readOptions);

        public KeyValuePair<byte[], byte[]>[] MultiGet(byte[][] keys, RdbColumnFamily[]? cf = null, ReadOptions? readOptions = null)
            => Instance.MultiGet(keys, cf?.Select(c => c.Handle).ToArray(), readOptions);

        public KeyValuePair<string, string>[] MultiGet(string[] keys, RdbColumnFamily[]? cf = null, ReadOptions? readOptions = null)
            => Instance.MultiGet(keys, cf?.Select(c => c.Handle).ToArray(), readOptions);

        public void Write(WriteBatch writeBatch, WriteOptions? writeOptions = null)
            => Instance.Write(writeBatch, writeOptions);

        public void Write(WriteBatchWithIndex writeBatch, WriteOptions? writeOptions = null)
            => Instance.Write(writeBatch, writeOptions);

        public void Remove(string key, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Remove(key, cf?.Handle, writeOptions);

        public void Remove(byte[] key, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Remove(key, cf?.Handle, writeOptions);

        public void Remove(byte[] key, long keyLength, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Remove(key, keyLength, cf?.Handle, writeOptions);

        public void Put(string key, string? value, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null, Encoding? encoding = null)
            => Instance.Put(key, value, cf?.Handle, writeOptions, encoding);

        public void Put(byte[] key, byte[]? value, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Put(key, value, cf?.Handle, writeOptions);

        public void Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Put(key, value, cf?.Handle, writeOptions);

        public void Put(byte[] key, long keyLength, byte[]? value, long valueLength, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Put(key, keyLength, value, valueLength, cf?.Handle, writeOptions);

        public void Merge(string key, string? value, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null, Encoding? encoding = null)
            => Instance.Merge(key, value, cf?.Handle, writeOptions, encoding);

        public void Merge(byte[] key, byte[]? value, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Merge(key, value, cf?.Handle, writeOptions);

        public void Merge(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Merge(key, value, cf?.Handle, writeOptions);

        public void Merge(byte[] key, long keyLength, byte[]? value, long valueLength, RdbColumnFamily? cf = null, WriteOptions? writeOptions = null)
            => Instance.Merge(key, keyLength, value, valueLength, cf?.Handle, writeOptions);

        public Iterator NewIterator(RdbColumnFamily? cf = null, ReadOptions? readOptions = null)
            => Instance.NewIterator(cf?.Handle, readOptions);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Instance.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
