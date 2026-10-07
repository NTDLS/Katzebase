using RocksDbSharp;

namespace NTDLS.Katzebase.Engine.IO
{
    /// <summary>
    /// RocksDB options shared by every Katzebase column family.
    /// </summary>
    internal static class RdbOptions
    {
        /// <summary>
        /// Creates the options used for every column family.
        ///
        /// The block cache is disabled: Katzebase has its own caching layer, so the block cache is redundant. More importantly,
        /// HyperClockCache (the default in RocksDB 8+) uses anonymous mmap which fails hard under memory pressure.
        ///
        /// A bloom filter (~10 bits per key, ~1% false positive rate) lets point lookups for keys that do not exist skip
        /// reading data blocks from every level. Those lookups are common: every insert checks whether its document key
        /// already exists, and index maintenance checks for existing entries. With cache_index_and_filter_blocks off (the
        /// default) the filters are held in memory alongside each table, so they work without a block cache.
        /// Filters apply to SST files written after this change; existing files gain them as they are compacted.
        /// </summary>
        public static ColumnFamilyOptions CreateColumnFamilyOptions()
            => new ColumnFamilyOptions()
                .SetBlockBasedTableFactory(new BlockBasedTableOptions()
                    .SetNoBlockCache(true)
                    .SetFilterPolicy(BloomFilterPolicy.Create(10, false))
                    .SetWholeKeyFiltering(true))
                .SetWalTtlSeconds(0);
    }
}
