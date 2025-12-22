// Copyright (c) 2025 Yuieii.

#nullable enable
using System.Runtime.CompilerServices;

namespace ue.Peak.TcnPatch.Core
{
    public static class NumberExtensions
    {
        extension(int self)
        {
            /// <summary>
            /// Returns the number of one-bits in the two-complement binary representation of the specified
            /// <see cref="int"/> value.
            /// This is sometimes referred to as the population count.
            /// </summary>
            public int BitCount
            {
                get
                {
                    self = self - ((self >>> 1) & 0x55555555);
                    self = (self & 0x33333333) + ((self >>> 2) & 0x33333333);
                    self = (self + (self >>> 4)) & 0x0f0f0f0f;
                    self = self + (self >>> 8);
                    self = self + (self >>> 16);
                    return self & 0x3f;
                }
            }

            /// <inheritdoc cref="BitCount"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetBitCount() => self.BitCount;
        }
    }
}