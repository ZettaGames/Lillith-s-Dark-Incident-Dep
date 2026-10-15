using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Levels
    private const int SkidLevel = 5;
    private const int DarkyLevel = 7;
    private const int LastCinematic = 9;
    private int _levelCounter = 1;

    // Score
    private float _totalScore;
    private int _currentStars;

    public float TotalScore
    {
        get => _totalScore;
        set => _totalScore = Mathf.Max(0f, value);
    }

    public int CurrentStars
    {
        get => _currentStars;
        set => _currentStars = value;
    }

    // Singleton instance of the Game Manager
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Lock the mouse cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        var vsync = PlayerPrefs.GetInt("VSync", 1) == 1;
        SetFrameRate(vsync);

        // Set the resolution to always be 1920x1080
        Screen.SetResolution(1920, 1080, true);
    }

    private void Update()
    {
        if (Instance != this || SceneTransitionManager.Instance == null ||
            SceneTransitionManager.Instance.IsLoading)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartLevel("04_FloeraLevel", 1);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            StartLevel("06_SkidLevel", 2);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            StartLevel("08_DarkyLevel", 3);
        }
    }

    public void ResetRun()
    {
        _levelCounter = 1;
        _totalScore = 0f;
        // Zero means there is no health carried over from a previous level.
        _currentStars = 0;
        LocalTime.TimeScale = 1f;
    }

    private void StartLevel(string sceneName, int level)
    {
        ResetRun();
        SetCurrentLevel(level);
        SceneTransitionManager.Instance.LoadLevel(sceneName);
    }

    public void SetFrameRate(bool vsync)
    {
        if (vsync)
        {
            QualitySettings.vSyncCount = 1;
        }

        if (!vsync)
        {
            QualitySettings.vSyncCount = 0;
        }
    }

    public void SetCurrentLevel(int level)
    {
        _levelCounter = level;
    }

    public int GetLevel()
    {
        _levelCounter++;
        if (_levelCounter == 2)
        {
            return SkidLevel;
        }
        else if (_levelCounter == 3)
        {
            return DarkyLevel;
        }
        else
        {
             return LastCinematic;
        }
    }
}
