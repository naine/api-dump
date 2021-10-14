// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ApiDump
{
    // Fast simple value-type list.
    // Care should be taken not to copy except when it will no longer be modified.
    struct FList<T> : IReadOnlyList<T>
    {
        private T[]? items;

        public int Count { readonly get; private set; }

        public FList(int capacity)
        {
            items = capacity > 0 ? GC.AllocateUninitializedArray<T>(capacity) : null;
            Count = 0;
        }

        public readonly ref T this[int index]
            => ref items![index];

        T IReadOnlyList<T>.this[int index]
            => items![index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (items is null || (uint)Count >= (uint)items.Length)
            {
                AddSlow(item);
            }
            else
            {
                items[Count++] = item;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddSlow(T item)
        {
            // TODO: In .NET 6 use Array.MaxLength
            int newCount = Count + 1;
            var newItems = GC.AllocateUninitializedArray<T>(
                Math.Max(newCount, (int)Math.Min(0x7FFFFFC7u,
                items is null || items.Length == 0 ? 8u : 2u * (uint)items.Length)));
            if (items is not null)
            {
                items.AsSpan(0, Count).CopyTo(newItems);
            }
            newItems[Count] = item;
            items = newItems;
            Count = newCount;
        }

        public void TrimEnd(int newSize)
        {
            if ((uint)newSize < (uint)Count)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    while (Count > newSize)
                    {
                        items![--Count] = default!;
                    }
                }
                else
                {
                    Count = newSize;
                }
            }
        }

        public readonly Enumerator GetEnumerator()
            => new(items, Count);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[]? array;
            private readonly int size;
            private int pos;

            internal Enumerator(T[]? array, int size)
            {
                this.array = array;
                this.size = size;
                pos = -1;
            }

            public T Current
                => array![pos];

            public bool MoveNext()
                => ++pos < size;

            object? IEnumerator.Current
                => Current;

            void IEnumerator.Reset()
                => pos = -1;

            public void Dispose()
            {
            }
        }
    }
}
