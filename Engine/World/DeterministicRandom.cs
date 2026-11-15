namespace JumpRunShoot.Engine.World;

/// <summary>
/// Fast deterministic 64-bit SplitMix64 pseudo-random number generator with FNV-1a string hashing.
/// </summary>
public struct DeterministicRandom
{
    private const ulong DefaultState = 0x853C49E6748FEA9BUL;
    private ulong _state;

    public DeterministicRandom(uint seed) : this((ulong)seed)
    {
    }

    public DeterministicRandom(ulong seed)
    {
        // Mix input seed so adjacent integers (e.g. chunk 1 vs 2) produce unrelated sequences.
        _state = Mix64(seed == 0 ? DefaultState : seed);
    }

    public DeterministicRandom(string seedText)
    {
        ulong hash = 14695981039346656037UL;
        if (!string.IsNullOrEmpty(seedText))
        {
            foreach (char c in seedText)
            {
                hash ^= (byte)(c & 0xFF);
                hash *= 1099511628211UL;
                uint high = (uint)(c >> 8);
                if (high != 0)
                {
                    hash ^= high;
                    hash *= 1099511628211UL;
                }
            }
            hash ^= (ulong)seedText.Length;
            hash *= 1099511628211UL;
        }
        _state = hash == 0 ? DefaultState : Mix64(hash);
    }

    private static ulong Mix64(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        ulong mixed = z ^ (z >> 31);
        return mixed == 0 ? DefaultState : mixed;
    }

    public ulong NextULong()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public uint NextUInt() => (uint)(NextULong() >> 32);

    public float NextFloat() => (NextULong() >> 40) * (1.0f / 16777216.0f);

    public int NextInt(int min, int max)
    {
        if (min >= max) return min;
        ulong range = (ulong)(max - min);
        return min + (int)(NextULong() % range);
    }

    public bool NextBool(float probability = 0.5f) => NextFloat() < probability;
}
