// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;

namespace ApiDump
{
    // Similar to string.Split(), but without all the allocations.
    ref struct SpanSplitter
    {
        private ReadOnlySpan<char> remaining;
        public ReadOnlySpan<char> Current { get; private set; }

        public SpanSplitter(ReadOnlySpan<char> input)
        {
            remaining = input;
            Current = default;
        }

        public readonly SpanSplitter GetEnumerator()
            => this;

        public bool MoveNext()
        {
            for (int start = 0; start < remaining.Length; ++start)
            {
                if (!char.IsWhiteSpace(remaining[start]))
                {
                    for (int end = start + 1; end < remaining.Length; ++end)
                    {
                        if (char.IsWhiteSpace(remaining[end]))
                        {
                            Current = remaining[start..end];
                            remaining = remaining.Slice(end + 1);
                            return true;
                        }
                    }
                    Current = remaining.Slice(start);
                    remaining = default;
                    return true;
                }
            }
            return false;
        }
    }
}
