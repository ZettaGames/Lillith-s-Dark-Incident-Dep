using UnityEngine;

/// <summary>
/// A class that provides a time scale that can be controlled locally, independently of Unity's global time scale.
/// </summary>
public static class LocalTime
{
    private static float _timeScale = 1f;
    private static float _resumeTimeScale = 1f;
    public static bool IsPaused { get; private set; }

    /// <summary>
    /// Value that represents the time scale. It is clamped between 0 and 1.
    /// </summary>
    public static float TimeScale
    {
        get { return IsPaused ? 0f : _timeScale; }
        set { _timeScale = Mathf.Clamp(value, 0f, 1f); }
    }

    // User pause freezes physics, animations and scaled waits as well as local movement.
    // The local scale remains independent for death and victory sequences.
    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;

        if (paused)
        {
            _resumeTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _resumeTimeScale;
        }
        IsPaused = paused;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _timeScale = 1f;
        _resumeTimeScale = 1f;
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
