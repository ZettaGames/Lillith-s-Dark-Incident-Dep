using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FloeraLevelManager : MonoBehaviour
{
    // Enemy Spawners
    [Header("Enemy Spawners")]
    [SerializeField] private SpawnerObstacles _obstacleSpawner;
    [SerializeField] private SpawnerFollowEnemy _followEnemySpawner;
    [SerializeField] private SpawnerPassEnemy _passEnemySpawner;

    // Phases
    [Header("Phases Settings")]
    [SerializeField] private float _phase1Duration;
    [SerializeField] private float _phase2Duration;
    [SerializeField] private TMP_Text _levelTime;
    [SerializeField] private TMP_Text _levelPhase;
    private float _totalTime;
    private bool _isTutorial;
    private bool _isPhase1;
    private bool _isPhase2;
    private bool _onLevel = false;

    // Obstacles
    private float _obsProbabilityP1 = 0.6f;
    private float _obsProbabilityP2 = 0.7f;
    private float _obsTime;

    // Follow Enemies
    private float _followEnemyDelayP1 = 0.2f;
    private float _followEnemyDelayP2 = 0.1f;
    private int _followEnemyAmountP1 = 3;
    private int _followEnemyAmountP2 = 4;
    private float _followEnemyTime;

    // Pass Enemies
    private float _passEnemyTime;

    // Coroutines
    private Coroutine _obstacleCoroutine;
    private Coroutine _followEnemyCoroutine;
    private Coroutine _passEnemyCoroutine;

    // Backgrouond Controller
    [Header("Background Controllers")]
    [SerializeField] private BackgroundOffsetController _grass;
    [SerializeField] private BackgroundOffsetController _logsRight;
    [SerializeField] private BackgroundOffsetController _logsLeft;
    [SerializeField] private BackgroundOffsetController _leafes;

    // Lillith Controller
    [Header("Lillith Controller")]
    [SerializeField] private LillithController _lillith;

    // Floera Sakr'
    [Header("Floera Sakr'")]
    [SerializeField] private GameObject _floeraStart;
    [SerializeField] private GameObject _floeraSplashArt;
    [SerializeField] private GameObject _floeraText;
    [SerializeField] private AudioClip _floeraScream;
    [SerializeField] private GameObject _shadowSquare;
    [SerializeField] private GameObject _FloeraSakr;
    [SerializeField] private GameObject _HealthBar;
    private Animator _floeraStartAnimator;

    [Header("Tutorial")]
    [SerializeField] private LillithController _lillithController;
    [SerializeField] private CanvasGroup _shootTutorial;
    [SerializeField] private CanvasGroup _superTutorial;
    [SerializeField] private CanvasGroup _shieldTutorial;
    [SerializeField] private InputActionAsset _inputActions;
    private InputAction _shootAction;
    private InputAction _superAction;
    private InputAction _shieldAction;

    private void Start()
    {
        _shootAction = _inputActions.FindAction("Shoot");
        _superAction = _inputActions.FindAction("Super");
        _shieldAction = _inputActions.FindAction("Shield");

        _floeraStartAnimator = _floeraStart.GetComponent<Animator>();

        _totalTime = _phase1Duration + _phase2Duration;

        int minutes = Mathf.FloorToInt(_totalTime / 60F);
        int seconds = Mathf.FloorToInt(_totalTime % 60F);
        _levelTime.text = $"Time: {minutes:00}:{seconds:00}";

        LevelScoreManager.Instance.OnLevel = false;

        StartCoroutine(LevelFlow());
    }

    private void Update()
    {
        if (_isTutorial)
        {
            int minutes = Mathf.FloorToInt(_totalTime / 60F);
            int seconds = Mathf.FloorToInt(_totalTime % 60F);
            _levelTime.text = $"Time: {minutes:00}:{seconds:00}";
            _levelPhase.text = "Tutorial!";
        }

        // Update the level time
        if (_onLevel)
        {
            int minutes = Mathf.FloorToInt(_totalTime / 60F);
            int seconds = Mathf.FloorToInt(_totalTime % 60F);
            _levelTime.text = $"Time: {minutes:00}:{seconds:00}";
            _totalTime -= Time.deltaTime * LocalTime.TimeScale;
        }

        // Update the level phase
        if (_isPhase1)
        {
            _levelPhase.text = "Phase 1";
        }
        else if (_isPhase2)
        {
            _levelPhase.text = "Phase 2";
        }

        if (_totalTime <= 0)
        {
            _levelTime.text = "Time: 00:00!";
            _levelPhase.text = "Extra Time";
        }

        // Update the text when the presentation starts
        if (!_isTutorial && !_isPhase1 && !_isPhase2)
        {
            _levelTime.text = "00:00!";
            _levelPhase.text = "Boss Incoming!";
        }
    }

    private IEnumerator LevelFlow()
    {
        StartCoroutine(Tutorial());

        while (!_onLevel)
        {
            yield return null;
        }

        Debug.Log("Phase 1 started");

        _isPhase1 = true;
        // 60% chance of spawning obstacles
        _obstacleCoroutine = StartCoroutine(SpawnObstacles(_obsProbabilityP1));
        // 3 follow enemies with 0.2s delay between each spawn
        _followEnemyCoroutine = StartCoroutine(SpawnFollowEnemies(_followEnemyDelayP1, _followEnemyAmountP1));
        // 1 pass enemy every random 1-4 seconds
        _passEnemyCoroutine = StartCoroutine(SpawnPassEnemies(_passEnemyTime));
        // Wait for phase 1 to end
        yield return StartCoroutine(WaitForPausedSeconds(_phase1Duration));
        _isPhase1 = false;

        // Stop all coroutines from phase 1
        StopCoroutine(_obstacleCoroutine);
        StopCoroutine(_followEnemyCoroutine);
        StopCoroutine(_passEnemyCoroutine);

        Debug.Log("Phase 1 ended");

        Debug.Log("Phase 2 started");

        _isPhase2 = true;
        // 70% chance of spawning obstacles
        _obstacleCoroutine = StartCoroutine(SpawnObstacles(_obsProbabilityP2));
        // 4 follow enemies with 0.1s delay between each spawn
        _followEnemyCoroutine = StartCoroutine(SpawnFollowEnemies(_followEnemyDelayP2, _followEnemyAmountP2));
        // 1 pass enemy every random 1-4 seconds
        _passEnemyCoroutine = StartCoroutine(SpawnPassEnemies(_passEnemyTime));
        // Wait for phase 2 to end
        yield return StartCoroutine(WaitForPausedSeconds(_phase2Duration));

        // Stop all coroutines from phase 2
        StopCoroutine(_obstacleCoroutine);
        StopCoroutine(_followEnemyCoroutine);
        StopCoroutine(_passEnemyCoroutine);

        Debug.Log("Phase 2 ended");

        // Wait until the spawners have no children
        while (_obstacleSpawner.gameObject.transform.childCount > 0)
        {
            bool allDisabled = true;
            foreach (Transform child in _obstacleSpawner.gameObject.transform)
            {
                if (child.gameObject.GetComponent<SpriteRenderer>().enabled)
                {
                    allDisabled = false;
                    break;
                }
            }

            if (allDisabled)
            {
                break;
            }

            yield return null;
        }
        _isPhase2 = false;

        yield return StartCoroutine(WaitForPausedSeconds(1.5f));

        // Stop the score
        LevelScoreManager.Instance.OnLevel = false;

        // Stop the background scrolling
        _grass.StopScrolling();
        _logsRight.StopScrolling();
        _logsLeft.StopScrolling();
        _leafes.StopScrolling();

        // Wait for the background to stop scrolling
        yield return StartCoroutine(WaitForPausedSeconds(1.5f));

        // Reset the player position
        _lillith.CanMove = false;
        _lillith.ReturnToStart();

        // Floera Sakr' presentation
        StartCoroutine(FloeraPresentation());
    }

    private IEnumerator SpawnObstacles(float probability)
    {
        while (_isPhase1 || _isPhase2)
        {
            // Pause if LocalTime is not 1
            while (LocalTime.TimeScale != 1)
            {
                yield return null;
            }

            // Repeat the process every 1.5-3.5 seconds
            _obsTime = Random.Range(1.5f, 3.5f);
            _obstacleSpawner.TrySpawn(probability);
            yield return new WaitForSeconds(_obsTime);
        }
    }

    private IEnumerator SpawnFollowEnemies(float delay, int amount)
    {
        while (_isPhase1 || _isPhase2)
        {
            // Pause if LocalTime is not 1
            while (LocalTime.TimeScale != 1)
            {
                yield return null;
            }

            // Repeat the process every 6.5-8.5 seconds
            _followEnemyTime = Random.Range(6.5f, 8.5f);
            _followEnemySpawner.SpawnWave(delay, amount);
            yield return new WaitForSeconds(_followEnemyTime);
        }
    }

    private IEnumerator SpawnPassEnemies(float time)
    {
        while (_isPhase1 || _isPhase2)
        {
            // Pause if LocalTime is not 1
            while (LocalTime.TimeScale != 1)
            {
                yield return null;
            }

            // Repeat the process every 1-4 seconds
            _passEnemyTime = Random.Range(1f, 4f);
            _passEnemySpawner.Spawn();
            yield return new WaitForSeconds(_passEnemyTime);
        }
    }

    private IEnumerator FloeraPresentation()
    {
        // Get the object named "SoundMusicManager" in the scene
        GameObject musicManager = GameObject.Find("SoundMusicManager");
        AudioSource audioSource = musicManager.GetComponent<AudioSource>();

        // Fade out music
        float fadeOutDuration = 3.5f;
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            // Pause if LocalTime is not 1
            while (LocalTime.TimeScale != 1)
            {
                yield return null;
            }
            audioSource.volume = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = 0f;

        // Remove the audio and return the volume to 1
        audioSource.clip = null;
        audioSource.volume = 1f;

        // Floeras position
        var floeraPosition = new Vector3(0f, 3f, 0f);
        var startPosition = _floeraStart.transform.position;
        float duration = 3.5f; // Duración fija en segundos
        elapsedTime = 0f;

        // Move floera to the screen
        while (elapsedTime < duration)
        {
            // Pause if LocalTime is not 1
            while (LocalTime.TimeScale != 1)
            {
                yield return null;
            }

            _floeraStart.transform.position = Vector3.Lerp(startPosition, floeraPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;

            // Introduce a small interruption every 1 second
            if (elapsedTime % 1.5f < Time.deltaTime)
            {
                _floeraStartAnimator.SetTrigger("transition");
                yield return new WaitForSeconds(1.7f);
                _floeraStartAnimator.SetTrigger("transition");
            }
        }
        _floeraStart.transform.position = floeraPosition;

        // Wait for the animation to finish
        yield return new WaitForSeconds(1.5f);

        // Make the scream animation and shake the gamepad and the screen
        _floeraStartAnimator.SetTrigger("scream");
        SoundFXManager.Instance.PlaySoundFXClip(_floeraScream, _floeraStart.transform, 100);
        yield return new WaitForSeconds(0.75f);
        _lillith.ShakeGamepad(0.75f, 0.75f, 1.75f);
        ScreenShake.Instance.Shake(1.75f, 0.3f);

        // Smoothly increase the opacity of the floera sprite and the shadow square
        float opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime;
            _floeraSplashArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Smoothly moves the splashart to a certain position while smoothing up the opacity of the text
        var splashartPosition = new Vector3(-1.75f, 0f, 0f);
        elapsedTime = 0f;
        duration = 3.25f;
        while (elapsedTime < duration)
        {
            _floeraText.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, elapsedTime / duration);
            if (!LocalTime.IsPaused)
                _floeraSplashArt.transform.position = Vector3.Lerp(_floeraSplashArt.transform.position, splashartPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Wait for the animation to finish
        yield return new WaitForSeconds(0.25f);

        // Smoothly decrease the opacity of the floera sprite and the shadow square
        opacity = 1f;
        while (opacity > 0)
        {
            opacity -= Time.deltaTime;
            _floeraSplashArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _floeraText.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Let the player move again
        _lillith.CanMove = true;

        // Destroy the Floera start object
        Destroy(_floeraStart);

        // Start the boss fight
        _levelTime.gameObject.SetActive(false);
        _FloeraSakr.SetActive(true);
        _HealthBar.SetActive(true);

        // Deactivate itself
        gameObject.SetActive(false);
    }

    private IEnumerator Tutorial()
    {
        _isTutorial = true;

        if (LocalTime.TimeScale == 0)
        {
            yield return null;
        }

        // Fade in the shoot tutorial
        float opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale;
            _shootTutorial.alpha = opacity;
            yield return null;
        }

        // Wait for the shoot action to be pressed
        while (!_shootAction.WasPressedThisFrame() || LocalTime.TimeScale == 0)
        {
            yield return null;
        }

        // Fade out the shoot tutorial
        while (opacity > 0)
        {
            opacity -= Time.deltaTime * LocalTime.TimeScale;
            _shootTutorial.alpha = opacity;
            yield return null;
        }

        // Fade in the super tutorial
        opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale;
            _superTutorial.alpha = opacity;
            yield return null;
        }

        // Reset the habilities cooldown
        _lillithController.ResetCooldowns();
        _lillithController.DestroyShields();

        // Wait for the super action to be pressed
        while (!_superAction.WasPressedThisFrame() || LocalTime.TimeScale == 0)
        {
            yield return null;
        }

        // Fade out the super tutorial
        while (opacity > 0)
        {
            opacity -= Time.deltaTime * LocalTime.TimeScale;
            _superTutorial.GetComponent<CanvasGroup>().alpha = opacity;
            yield return null;
        }

        // Fade in the shield tutorial
        opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale;
            _shieldTutorial.GetComponent<CanvasGroup>().alpha = opacity;
            yield return null;
        }

        // Reset the habilities cooldown
        _lillithController.ResetCooldowns();
        _lillithController.DestroyShields();

        // Wait for the shield action to be pressed
        while (!_shieldAction.WasPressedThisFrame() || LocalTime.TimeScale == 0)
        {
            yield return null;
        }

        // Fade out the shield tutorial
        while (opacity > 0)
        {
            opacity -= Time.deltaTime * LocalTime.TimeScale;
            _shieldTutorial.GetComponent<CanvasGroup>().alpha = opacity;
            yield return null;
        }

        // Reset the habilities cooldown
        _lillithController.ResetCooldowns();
        _lillithController.DestroyShields();

        // Reanude the score
        LevelScoreManager.Instance.OnLevel = true;

        // Start the level
        _isTutorial = false;
        _onLevel = true;
    }

    private IEnumerator WaitForPausedSeconds(float seconds)
    {
        float elapsedTime = 0f;
        while (elapsedTime < seconds)
        {
            if (LocalTime.TimeScale == 1)
            {
                elapsedTime += Time.deltaTime;
            }
            yield return null;
        }
    }
}

