using System;
using System.Collections.Generic;

namespace JumpRunShoot.Engine.Input;

public sealed class InputManager : IInputSource
{
    private const int ActionCount = (int)GameAction.Count;

    private readonly bool[] _isDown = new bool[ActionCount];
    private readonly bool[] _justPressed = new bool[ActionCount];
    private readonly bool[] _justReleased = new bool[ActionCount];

    private readonly bool[] _rawDown = new bool[ActionCount];
    private readonly bool[] _pressedEvents = new bool[ActionCount];
    private readonly bool[] _releasedEvents = new bool[ActionCount];

    private readonly Dictionary<string, GameAction> _keyMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _heldKeyCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pressedKeyCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _justPressedKeyCodes = new(StringComparer.OrdinalIgnoreCase);
    private bool _anyKeyPressedEvent;

    public bool AnyKeyJustPressed { get; private set; }

    public InputManager()
    {
        SetDefaultBindings();
    }

    public void SetDefaultBindings()
    {
        _keyMap.Clear();

        // Classic Apogee DOS Bindings
        BindKey("ControlLeft", GameAction.Jump);
        BindKey("ControlRight", GameAction.Jump);
        BindKey("AltLeft", GameAction.Shoot);
        BindKey("AltRight", GameAction.Shoot);
        BindKey("ShiftLeft", GameAction.Run);
        BindKey("ShiftRight", GameAction.Run);
        BindKey("ArrowLeft", GameAction.MoveLeft);
        BindKey("ArrowRight", GameAction.MoveRight);
        BindKey("ArrowUp", GameAction.MoveUp);
        BindKey("ArrowDown", GameAction.MoveDown);

        // Modern / Laptop Fallbacks
        BindKey("Space", GameAction.Jump);
        BindKey("KeyZ", GameAction.Jump);
        BindKey("KeyX", GameAction.Shoot);
        BindKey("KeyJ", GameAction.Shoot);
        BindKey("KeyK", GameAction.Run);
        BindKey("KeyA", GameAction.MoveLeft);
        BindKey("KeyD", GameAction.MoveRight);
        BindKey("KeyW", GameAction.MoveUp);
        BindKey("KeyS", GameAction.MoveDown);

        // Pause (Strictly P key)
        BindKey("KeyP", GameAction.Pause);
    }

    public void BindKey(string code, GameAction action) => _keyMap[code] = action;

    public bool HandleKeyDown(string code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            if (_heldKeyCodes.Add(code))
            {
                _anyKeyPressedEvent = true;
                _pressedKeyCodes.Add(code);
            }
        }

        if (_keyMap.TryGetValue(code, out var action))
        {
            int idx = (int)action;
            if (!_rawDown[idx])
            {
                _pressedEvents[idx] = true;
            }
            _rawDown[idx] = true;
            return true;
        }
        return false;
    }

    public bool HandleKeyUp(string code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            _heldKeyCodes.Remove(code);
        }

        if (_keyMap.TryGetValue(code, out var action))
        {
            int idx = (int)action;
            _rawDown[idx] = false;
            _releasedEvents[idx] = true;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        Array.Clear(_isDown);
        Array.Clear(_justPressed);
        Array.Clear(_justReleased);
        Array.Clear(_rawDown);
        Array.Clear(_pressedEvents);
        Array.Clear(_releasedEvents);
        _heldKeyCodes.Clear();
        _pressedKeyCodes.Clear();
        _justPressedKeyCodes.Clear();
        _anyKeyPressedEvent = false;
        AnyKeyJustPressed = false;
    }

    public void AdvanceFrame()
    {
        bool anyActionJustPressed = false;
        for (int i = 0; i < ActionCount; i++)
        {
            _isDown[i] = _rawDown[i];
            _justPressed[i] = _pressedEvents[i];
            _justReleased[i] = _releasedEvents[i];

            if (_pressedEvents[i]) anyActionJustPressed = true;

            _pressedEvents[i] = false;
            _releasedEvents[i] = false;
        }

        _justPressedKeyCodes.Clear();
        foreach (var code in _pressedKeyCodes)
        {
            _justPressedKeyCodes.Add(code);
        }
        _pressedKeyCodes.Clear();

        AnyKeyJustPressed = _anyKeyPressedEvent || anyActionJustPressed;
        _anyKeyPressedEvent = false;
    }

    public bool IsDown(GameAction action) => _isDown[(int)action];
    public bool JustPressed(GameAction action) => _justPressed[(int)action];
    public bool JustReleased(GameAction action) => _justReleased[(int)action];

    public bool IsKeyJustPressed(string code) => _justPressedKeyCodes.Contains(code);
    public bool IsKeyDown(string code) => _heldKeyCodes.Contains(code);
}
