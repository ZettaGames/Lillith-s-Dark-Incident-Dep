using System.Collections.Generic;
using UnityEngine;

public class InkManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> _inkSpots = new List<GameObject>();
    [SerializeField] private float _timeToShowInk;
    private const float HoldDuration = 5f;
    private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
    private float[] _remainingTimes;
    private int _lastSpot = -1;

    public static InkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Ink belongs to this level and must not follow the player into other scenes.
        foreach (var spot in _inkSpots)
        {
            if (spot == null) continue;
            var renderer = spot.GetComponent<SpriteRenderer>();
            if (renderer == null || _renderers.Contains(renderer)) continue;
            _renderers.Add(renderer);
            SetOpacity(renderer, 0f);
        }
        _remainingTimes = new float[_renderers.Count];
    }

    public void ShowInkSpot()
    {
        if (LocalTime.TimeScale <= 0f || _renderers.Count == 0) return;

        // Choose uniformly among hidden spots before refreshing a visible one.
        int selected = -1;
        int available = 0;
        for (int i = 0; i < _remainingTimes.Length; i++)
        {
            if (_remainingTimes[i] <= 0f && Random.Range(0, ++available) == 0)
                selected = i;
        }

        if (selected < 0)
        {
            // When all spots are visible, avoid refreshing the same spot twice in a row.
            selected = Random.Range(0, Mathf.Max(1, _renderers.Count - 1));
            if (_renderers.Count > 1 && selected >= _lastSpot) selected++;
        }

        _lastSpot = selected;
        _remainingTimes[selected] = HoldDuration + Mathf.Max(0f, _timeToShowInk);
        SetOpacity(_renderers[selected], 1f);
    }

    private void Update()
    {
        if (_remainingTimes == null || LocalTime.TimeScale <= 0f) return;
        for (int i = 0; i < _remainingTimes.Length; i++)
        {
            if (_remainingTimes[i] <= 0f) continue;
            _remainingTimes[i] = Mathf.Max(0f,
                _remainingTimes[i] - Time.deltaTime * LocalTime.TimeScale);
            float alpha = _timeToShowInk > 0f
                ? Mathf.Clamp01(_remainingTimes[i] / _timeToShowInk)
                : (_remainingTimes[i] > 0f ? 1f : 0f);
            SetOpacity(_renderers[i], alpha);
        }
    }

    private static void SetOpacity(SpriteRenderer renderer, float opacity)
    {
        var color = renderer.color;
        color.a = opacity;
        renderer.color = color;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
