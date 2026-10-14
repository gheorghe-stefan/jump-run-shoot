namespace JumpRunShoot.Engine.Entities;

public enum Direction : byte
{
    Right = 0,
    Left = 1
}

public enum PlayerState : byte
{
    Idle = 0,
    Walking = 1,
    Running = 2,
    Jumping = 3,
    Falling = 4,
    Shooting = 5
}
