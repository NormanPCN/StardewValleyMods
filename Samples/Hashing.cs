using System.Runtime.CompilerServices;

namespace Misc
{
    class Hashing
    {
        internal ulong Hash {
            [MethodImpl(Runtime.MethodImpl.Hot)]
            get {
                if (ReferenceDataInternal is null) {
                    throw new NullReferenceException(nameof(ReferenceDataInternal));
                }

                ulong hash = HashInternal;
                if (hash == 0) {
                    hash = Hashing.Combine(
                        SpriteDataHash,
                        Bounds.Extent.GetLongHashCode(),
                        BlendEnabled.GetLongHashCode(),
                        ExpectedScale.GetLongHashCode(),
                        IsWater.GetLongHashCode(),
                        IsFont.GetLongHashCode(),
                        Reference.Format.GetLongHashCode(),
                        Scaler.GetLongHashCode(),
                        ScalerGradient.GetLongHashCode()
                    );

                }
                HashInternal = hash;
                return hash;// ^ (ulong)ExpectedScale.GetHashCode();
            }
        }

        [MethodImpl(Runtime.MethodImpl.Hot)]
        internal static ulong GetHash(SpriteInfo input, TextureType textureType) {
            // Need to make Hashing.CombineHash work better.
            var hash = input.Hash;

            if (Config.Resample.EnableDynamicScale) {
                hash = Hashing.Combine(hash, Hashing.Rehash(input.ExpectedScale));
            }

            if (textureType == TextureType.Sprite) {
                hash = Hashing.Combine(hash, input.Bounds.Extent.GetLongHashCode());
            }
            return hash;
        }

        [MethodImpl(Runtime.MethodImpl.Hot)]
        internal static ulong Combine(params object?[] hashes) {
            ulong hash = Default;

            foreach (var subHash in hashes) {
                hash = subHash switch {
                    int i => Accumulate(hash, i),
                    uint i => Accumulate(hash, (int)i),
                    long i => Accumulate(hash, (ulong)i),
                    ulong i => Accumulate(hash, i),
                    string s => Accumulate(hash, s.GetSafeHash()),
                    StringBuilder s => Accumulate(hash, s.GetSafeHash()),
                    null => Accumulate(hash, Null),
                    _ => Accumulate(hash, subHash.GetHashCode()),
                };
            }
            return hash;
        }

        internal static ulong Rehash(ulong value) {
            if (value == 0) {
                value = 0x9e3779b97f4a7c15UL; // ⌊2^64 / Φ⌋
            }
            value = (value ^ value >> 30) * 0xbf58476d1ce4e5b9UL;
            value = (value ^ value >> 27) * 0x94d049bb133111ebUL;
            value ^= value >> 31;
            return value;
        }

        internal static ulong Accumulate(ulong hash, ulong hashend) => hash ^ hashend + Default + (hash << 6) + (hash >> 2);
    }
}