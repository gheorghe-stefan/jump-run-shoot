namespace JumpRunShoot.Engine.Input;

public interface IInputSource
{
    bool IsDown(GameAction action);
    bool JustPressed(GameAction action);
    bool JustReleased(GameAction action);
}
