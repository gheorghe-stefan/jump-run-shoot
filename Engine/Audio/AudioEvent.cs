using System;

namespace JumpRunShoot.Engine.Audio;

/// <summary>
/// Bitmask flags representing sound effects to play in the current animation frame.
/// Enables zero-allocation C# to JS sound dispatch via a single integer.
/// </summary>
[Flags]
public enum AudioEvent : int
{
    None = 0,
    PlayerShoot = 1 << 0,     // 1: Snappy retro blaster zap/chirp
    PlayerStep = 1 << 1,      // 2: Walking footstep tap
    PlayerRunStep = 1 << 2,   // 4: Running footstep tap (accelerated cadence & higher pitch)
    PlayerJump = 1 << 3,      // 8: Springy ascending retro tone sweep
    MonsterShoot = 1 << 4,    // 16: Robotic enemy weapon discharge
    MonsterDie = 1 << 5,      // 32: Crunchy retro explosion / metallic disintegrator burst
    Pickup = 1 << 6,          // 64: High-frequency reward chime
    PlayerDamage = 1 << 7     // 128: Harsh low-frequency damage buzz
}
