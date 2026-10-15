using TMPro;
using UnityEngine;

public class LevelScoreManager : MonoBehaviour
{
    private float _score;
    private bool _onLevel = true;

    private int _currentHealth;

    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private LillithHealthManager _lillithHealthManager;

    public static LevelScoreManager Instance { get; private set; }

    public bool OnLevel
    {
        get => _onLevel;
        set => _onLevel = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _score = Mathf.Max(0f, GameManager.Instance.TotalScore);

        if (GameManager.Instance.CurrentStars != 0)
        {
            _lillithHealthManager.CurrentStars = GameManager.Instance.CurrentStars;
        }
    }

    void Update()
    {
        _currentHealth = _lillithHealthManager.CurrentStars;

        _scoreText.text = $"{(int)_score:D9}";

        if (_onLevel)
        {
            // Constantly update the score text (750 per second)
            _score += 750 * Time.deltaTime * LocalTime.TimeScale;
        }
    }

    public void HitPenalty()
    {
        _score = Mathf.Max(0f, _score - Mathf.Max(_score * 0.05f, 25000f));
    }

    public void EnemyHitBonus()
    {
        _score += 250;
    }

    public void SuperHitBonus()
    {
        _score += 1000;
    }

    public void BossHitBonus()
    {
        _score += Mathf.Max(5000 - (_score * 0.1f), 125);
    }

    public void ScoreBonus()
    {
        _score += 1500;

    }

    public void StarBonus()
    {
        _score += _score * 0.15f;
    }

    public void SaveScore()
    {
        GameManager.Instance.TotalScore = _score;
        GameManager.Instance.CurrentStars = _currentHealth;
    }
}
