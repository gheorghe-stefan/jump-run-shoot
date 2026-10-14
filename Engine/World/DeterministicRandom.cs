namespace JumpRunShoot.Engine.World;

public struct DeterministicRandom
{
    private uint _state;

    public DeterministicRandom(uint seed)
    {
        _state = seed == 0 ? 0x853C49E6 : seed;
    }

    public DeterministicRandom(string seedText)
    {
        uint hash = 2166136261;
        if (!string.IsNullOrEmpty(seedText))
        {
            foreach (char c in seedText)
            {
                hash = (hash ^ c) * 16777619;
            }
        }
        _state = hash == 0 ? 0x853C49E6 : hash;
    }

    public uint NextUInt()
    {
        _state += 0x6D2B79F5;
        uint t = _state;
        t = (t ^ (t >> 15)) * (t | 1);
        t ^= t + (t ^ (t >> 7)) * (t | 61);
        return t ^ (t >> 14);
    }

    public float NextFloat() => (NextUInt() >> 8) * (1.0f / 16777216.0f);

    public int NextInt(int min, int max)
    {
        if (min >= max) return min;
        uint range = (uint)(max - min);
        return min + (int)(NextUInt() % range);
    }

    public bool NextBool(float probability = 0.5f) => NextFloat() < probability;
}
