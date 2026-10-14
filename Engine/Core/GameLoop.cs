namespace JumpRunShoot.Engine.Core;

public sealed class GameLoop
{
    public const float FixedDeltaTime = 1.0f / 60.0f;
    public const float MaxFrameTime = 0.10f;

    private readonly GameEngine _engine;
    private double _lastTimestamp = 0;
    private float _accumulator = 0.0f;
    private int _frameCount = 0;
    private double _fpsTimer = 0;

    public GameEngine Engine => _engine;
    public bool IsRunning { get; private set; }

    public GameLoop(GameEngine engine)
    {
        _engine = engine;
    }

    public void Start()
    {
        _lastTimestamp = 0;
        _accumulator = 0;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Tick(double timestamp)
    {
        if (!IsRunning) return;

        if (_lastTimestamp <= 0)
        {
            _lastTimestamp = timestamp;
            _fpsTimer = timestamp;
            return;
        }

        float frameTime = (float)((timestamp - _lastTimestamp) / 1000.0);
        _lastTimestamp = timestamp;

        if (frameTime > MaxFrameTime)
        {
            frameTime = MaxFrameTime;
        }

        _accumulator += frameTime;

        while (_accumulator >= FixedDeltaTime)
        {
            _engine.Update(FixedDeltaTime);
            _accumulator -= FixedDeltaTime;
        }

        _engine.Render();

        _frameCount++;
        if (timestamp - _fpsTimer >= 1000.0)
        {
            _engine.Fps = _frameCount;
            _frameCount = 0;
            _fpsTimer = timestamp;
        }
    }
}
