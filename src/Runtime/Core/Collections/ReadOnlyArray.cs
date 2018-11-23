// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ML.Probabilistic.Core.Collections
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a read only array. Having it as a struct avoids allocations on heap
    /// and helps devirtualizing calls at call sites.
    /// </summary>
    public struct ReadOnlyArray<T> : IReadOnlyList<T>
    {
        private readonly T[] array;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyArray{T}"/> structure.
        /// TODO
        /// </summary>
        public ReadOnlyArray(T[] array)
        {
            this.array = array;
        }

        public bool IsNull => this.array == null;

        public T this[int index] => this.array[index];

        public int Count => this.array.Length;


        /// <summary>
        /// Returns enumerator over elements of array.
        /// </summary>
        /// <remarks>
        /// This is value-type unvirtualized version of enumerator that is used by compiler
        /// in foreach loops.
        /// </remarks>
        public ReadOnlyArrayEnumerator<T> GetEnumerator() =>
            new ReadOnlyArrayEnumerator<T>(this, 0, this.array.Length);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            new ReadOnlyArrayEnumerator<T>(this, 0, this.array.Length);

        IEnumerator IEnumerable.GetEnumerator() =>
            new ReadOnlyArrayEnumerator<T>(this, 0, this.array.Length);

        public static implicit operator ReadOnlyArray<T>(T[] array) =>
            new ReadOnlyArray<T>(array);
    }

    public struct ReadOnlyArrayView<T> : IReadOnlyList<T>
    {
        private ReadOnlyArray<T> array;

        private int begin;

        private int end;

        public ReadOnlyArrayView(ReadOnlyArray<T> array, int begin, int end)
        {
            this.array = array;
            this.begin = begin;
            this.end = end;
        }

        public T this[int index] => this.array[this.begin + index];

        public int Count => this.end - this.begin;

        /// <summary>
        /// Returns enumerator over elements of array.
        /// </summary>
        /// <remarks>
        /// This is value-type unvirtualized version of enumerator that is used by compiler
        /// in foreach loops.
        /// </remarks>
        public ReadOnlyArrayEnumerator<T> GetEnumerator() =>
            new ReadOnlyArrayEnumerator<T>(this.array, this.begin, this.end);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            new ReadOnlyArrayEnumerator<T>(this.array, this.begin, this.end);

        IEnumerator IEnumerable.GetEnumerator() =>
            new ReadOnlyArrayEnumerator<T>(this.array, this.begin, this.end);
    }

    public struct ReadOnlyArrayEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        private readonly ReadOnlyArray<T> array;

        private readonly int begin;

        private readonly int end;

        private int pointer;

        public ReadOnlyArrayEnumerator(ReadOnlyArray<T> array, int begin, int end)
        {
            this.array = array;
            this.begin = begin;
            this.end = end;
            this.pointer = begin - 1;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            ++this.pointer;
            return this.pointer < this.end;
        }

        public T Current => this.array[this.pointer];

        object IEnumerator.Current => this.Current;

        void IEnumerator.Reset()
        {
            this.pointer = this.begin - 1;
        }
    }
}
