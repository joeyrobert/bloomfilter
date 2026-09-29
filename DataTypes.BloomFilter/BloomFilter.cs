/* Copyright (c) 2009 Joseph Robert. All rights reserved.
 *
 * This file is part of BloomFilter.NET.
 *
 * BloomFilter.NET is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or (at your option) any later
 * version.
 *
 * BloomFilter.NET is distributed in the hope that it will be
 * useful, but WITHOUT ANY WARRANTY; without even the implied
 * warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with BloomFilter.NET.  If not, see
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace DataTypes
{
    /// <summary>
    /// A Bloom filter is a space-efficient probabilistic data structure
    /// that is used to test whether an element is a member of a set. False
    /// positives are possible, but false negatives are not. Elements can
    /// be added to the set, but not removed.
    /// </summary>
    /// <remarks>
    /// Instances are not thread-safe for concurrent writes. Membership is
    /// based on <see cref="IEqualityComparer{T}.GetHashCode(T)"/>, which for
    /// some types (e.g. <see cref="string"/> on .NET Core) is randomized per
    /// process, so filters must not be persisted across processes.
    /// </remarks>
    /// <typeparam name="T">Data type to be classified</typeparam>
    public class BloomFilter<T>
    {
        private readonly int _bitSize;
        private readonly int _setSize;
        private readonly BitArray _bitArray;
        private readonly IEqualityComparer<T> _comparer;
        private int _numberOfHashes;

        #region Constructors
        /// <summary>
        /// Initializes the bloom filter and sets the optimal number of hashes.
        /// </summary>
        /// <param name="bitSize">Size of the bloom filter in bits (m)</param>
        /// <param name="setSize">Expected size of the set (n)</param>
        /// <param name="comparer">Optional comparer providing the hash code</param>
        public BloomFilter(int bitSize, int setSize, IEqualityComparer<T>? comparer = null)
            : this(bitSize, setSize, OptimalNumberOfHashes(bitSize, setSize), comparer)
        {
        }

        /// <summary>
        /// Initializes the bloom filter with a manual number of hashes.
        /// </summary>
        /// <param name="bitSize">Size of the bloom filter in bits (m)</param>
        /// <param name="setSize">Expected size of the set (n)</param>
        /// <param name="numberOfHashes">Number of hashing functions (k)</param>
        /// <param name="comparer">Optional comparer providing the hash code</param>
        public BloomFilter(int bitSize, int setSize, int numberOfHashes, IEqualityComparer<T>? comparer = null)
        {
            if (bitSize <= 0) throw new ArgumentOutOfRangeException(nameof(bitSize));
            if (setSize <= 0) throw new ArgumentOutOfRangeException(nameof(setSize));
            if (numberOfHashes <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfHashes));

            _bitSize = bitSize;
            _setSize = setSize;
            _numberOfHashes = numberOfHashes;
            _bitArray = new BitArray(bitSize);
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Number of hashing functions (k)
        /// </summary>
        public int NumberOfHashes
        {
            get => _numberOfHashes;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
                _numberOfHashes = value;
            }
        }

        /// <summary>
        /// Size of the bloom filter in bits (m)
        /// </summary>
        public int BitSize => _bitSize;

        /// <summary>
        /// Expected size of the set (n)
        /// </summary>
        public int SetSize => _setSize;
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds an item to the bloom filter.
        /// </summary>
        /// <param name="item">Item to be added</param>
        public void Add(T item)
        {
            Probe(item, out uint h, out uint step);
            for (int i = 0; i < _numberOfHashes; i++)
            {
                _bitArray[(int)(h % (uint)_bitSize)] = true;
                h += step;
            }
        }

        /// <summary>
        /// Adds every item in the sequence to the bloom filter.
        /// </summary>
        /// <param name="items">Items to be added</param>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (T item in items)
                Add(item);
        }

        /// <summary>
        /// Checks whether an item is probably in the set. False positives
        /// are possible, but false negatives are not.
        /// </summary>
        /// <param name="item">Item to be checked</param>
        /// <returns>True if the set probably contains the item</returns>
        public bool Contains(T item)
        {
            Probe(item, out uint h, out uint step);
            for (int i = 0; i < _numberOfHashes; i++)
            {
                if (!_bitArray[(int)(h % (uint)_bitSize)])
                    return false;
                h += step;
            }

            return true;
        }

        /// <summary>
        /// Checks if any item in the sequence is probably in the set.
        /// </summary>
        /// <param name="items">Items to be checked</param>
        /// <returns>True if the bloom filter contains any of the items</returns>
        public bool ContainsAny(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (T item in items)
            {
                if (Contains(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if all items in the sequence are probably in the set.
        /// </summary>
        /// <param name="items">Items to be checked</param>
        /// <returns>True if the bloom filter contains all of the items</returns>
        public bool ContainsAll(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (T item in items)
            {
                if (!Contains(item))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Removes all items from the bloom filter.
        /// </summary>
        public void Clear()
        {
            _bitArray.SetAll(false);
        }

        /// <summary>
        /// Computes the probability of encountering a false positive for a
        /// filter holding the expected number of items.
        /// </summary>
        /// <returns>Probability of a false positive</returns>
        public double FalsePositiveProbability()
        {
            return Math.Pow(1 - Math.Exp(-(double)_numberOfHashes * _setSize / _bitSize), _numberOfHashes);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Derives the start and step of the probe sequence using double
        /// hashing (Kirsch-Mitzenmacher) from a single hash code.
        /// </summary>
        private void Probe(T item, out uint start, out uint step)
        {
            int code = item is null ? 0 : _comparer.GetHashCode(item);
            // Mix the 32-bit hash (murmur3 finalizer) to get two well-spread values.
            uint x = (uint)code;
            x ^= x >> 16; x *= 0x85ebca6b;
            x ^= x >> 13; x *= 0xc2b2ae35;
            x ^= x >> 16;
            uint y = x * 0x9e3779b1 + 0x7f4a7c15;
            y ^= y >> 15;
            start = x;
            step = y | 1; // odd step
        }

        /// <summary>
        /// Calculates the optimal number of hashes, k = (m / n) ln 2.
        /// </summary>
        private static int OptimalNumberOfHashes(int bitSize, int setSize)
        {
            if (bitSize <= 0) throw new ArgumentOutOfRangeException(nameof(bitSize));
            if (setSize <= 0) throw new ArgumentOutOfRangeException(nameof(setSize));
            return Math.Max(1, (int)Math.Round((double)bitSize / setSize * Math.Log(2.0)));
        }
        #endregion
    }
}
