// KeyJaya: Read-only, indexed dictionary access with exact-key checks and a bounded lookup cache.
// Index/data pairs are immutable inputs. A hash match alone never authorizes returning a record.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace KeyJaya
{
    /// <summary>
    /// Binary-searches an immutable FNV-1a index, then verifies the full stored key.
    /// Owns its streams; dispose it when an engine is replaced or a load fails.
    /// </summary>
    public sealed class IndexedStore : IDisposable
    {
        // Each little-endian index row is hash:u64, data offset:i64, byte length:i32.
        // Data records contain UTF-8 key, NUL, then UTF-8 value. These byte offsets
        // are why data files must never undergo text/newline normalization.
        readonly FileStream index, data;
        readonly BinaryReader reader;
        // File positions are shared mutable state even though the files are read-only.
        // The same lock protects seeks, reads, and the bounded hit/miss cache.
        readonly object sync = new object ();
        readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly ulong[] hashes;
        readonly long recordCount;
        public IndexedStore(string root, string stem, bool keepHashes = false)
        {
            try
            {
                index = new FileStream(Path.Combine(root, stem + ".idx"), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
                reader = new BinaryReader(index);
                if (index.Length % 20 != 0)
                    throw new InvalidDataException("Invalid index: " + stem);
                recordCount = index.Length / 20;
                data = new FileStream(Path.Combine(root, stem + ".dat"), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
                if (keepHashes)
                {
                    hashes = new ulong[recordCount];
                    byte[] block = new byte[20 * 1024];
                    for (int first = 0; first < hashes.Length;)
                    {
                        int count = Math.Min(1024, hashes.Length - first), bytes = count * 20, read = 0;
                        while (read < bytes)
                        {
                            int n = index.Read(block, read, bytes - read);
                            if (n == 0)
                                throw new EndOfStreamException();
                            read += n;
                        }

                        // The index is little-endian, as are all supported Windows x64 builds.
                        for (int i = 0; i < count; i++)
                            hashes[first + i] = BitConverter.ToUInt64(block, i * 20);
                        first += count;
                    }
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Fast negative filter when hashes were retained in memory. True is only
        /// a candidate match; Get still verifies the full key and record bounds.
        /// </summary>
        public bool MayContain(string key)
        {
            return hashes == null || Array.BinarySearch(hashes, Hash(key)) >= 0;
        }

        public static ulong Hash(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            ulong h = 14695981039346656037UL;
            foreach (char c in key)
            {
                // Most dictionary probes are ASCII. Keep UTF-8 fallback semantics,
                // including replacement of invalid surrogate sequences, unchanged.
                if (c > 127)
                {
                    h = 14695981039346656037UL;
                    foreach (byte b in Encoding.UTF8.GetBytes(key))
                        h = unchecked((h ^ b) * 1099511628211UL);
                    return h;
                }

                h = unchecked((h ^ c) * 1099511628211UL);
            }

            return h;
        }

        public string Get(string key)
        {
            lock (sync)
            {
                string cached;
                if (cache.TryGetValue(key, out cached))
                    return cached;
                ulong h = Hash(key);
                long low = 0, high = recordCount;
                while (low < high)
                {
                    long mid = (low + high) / 2;
                    ulong x;
                    if (hashes != null)
                        x = hashes[mid];
                    else
                    {
                        index.Position = mid * 20;
                        x = reader.ReadUInt64();
                    }

                    if (x < h)
                        low = mid + 1;
                    else
                        high = mid;
                }

                if (hashes != null && (low == recordCount || hashes[low] != h))
                    return null;
                string result = null;
                for (long j = low; j < recordCount; j++)
                {
                    index.Position = j * 20;
                    if (reader.ReadUInt64() != h)
                        break;
                    long offset = reader.ReadInt64();
                    int length = reader.ReadInt32();
                    if (offset < 0 || length < 1 || length > 4000000 || offset > data.Length - length)
                        throw new InvalidDataException("Invalid record");
                    byte[] record = new byte[length];
                    data.Position = offset;
                    int done = 0;
                    while (done < length)
                    {
                        int n = data.Read(record, done, length - done);
                        if (n == 0)
                            throw new EndOfStreamException();
                        done += n;
                    }

                    int split = Array.IndexOf(record, (byte)0);
                    if (split >= 0 && Encoding.UTF8.GetString(record, 0, split) == key)
                    {
                        result = Encoding.UTF8.GetString(record, split + 1, length - split - 1);
                        break;
                    }
                }

                if (cache.Count >= 512)
                    cache.Clear();
                cache[key] = result;
                return result;
            }
        }

        public void Dispose()
        {
            if (reader != null)
                reader.Dispose();
            else if (index != null)
                index.Dispose();
            if (data != null)
                data.Dispose();
        }
    }
}
