using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class SkidLevelManager : MonoBehaviour
{
    // Enemy Spawners
    [Header("Enemy Spawners")]
    [SerializeField] private SpawnerObstacles _obstacleSpawnerV1;
    [SerializeField] private SpawnerObstacles _obstacleSpawnerV2;
    [SerializeField] private SpawnerFollowEnemy _followEnemySpawner;
    [SerializeField] private SpawnerFollowEnemy _crazyEnemySpawner;
    [SerializeField] private SpawnerPassEnemy _passEnemySpawner;

    // Phases
    [SerializeField] private float _phase1Duration;
    [SerializeField] private float _phase2Duration;
    [SerializeField] private TMP_Text _levelTime;
    [SerializeField] private TMP_Text _levelPhase;
    private float _totalTime;
    private bool _isPhase1;
    private bool _isPhase2;

    // Obstacles
    private float _obsProbabilityP1 = 0.65f;
    private float _obsProbabilityP2 = 0.8f;
    private float _obsTime;

    // Follow Enemies
    private float _followEnemyDelayP1 = 0.2f;
    private float _followEnemyDelayP2 = 0.1f;
    private int _followEnemyAmountP1 = 3;
    private int _followEnemyAmountP2 = 4;
    private float _followEnemyTime;

    // Crazy Enemies
    private float _crazyEnemyDelayP1 = 0.4f;
    private float _crazyEnemyDelayP2 = 0.2f;
    private int _crazyEnemyAmountP1 = 2;
    private int _crazyEnemyAmountP2 = 3;
    private float _crazyEnemyTime;

    // Pass Enemies
    private float _passEnemyTime;

    // Coroutines
    private Coroutine _obstacleCoroutine;
    private Coroutine _followEnemyCoroutine;
    private Coroutine _passEnemyCoroutine;
    private Coroutine _crazyEnemyCoroutine;

    // Backgrouond Controller
    [Header("Background Controllers")]
    [SerializeField] private BackgroundOffsetController _water;

    // Lillith Controller
    [Header("Lillith Controller")]
    [SerializeField] private LillithController _lillith;

    // Skid L' Kuin
    [Header("Skid L' Kuin")]
    [SerializeField] private GameObject _skidStart;
    [SerializeField] private GameObject _skidSplashArt;
    [SerializeField] private GameObject _skidText;
    [SerializeField] private GameObject _shadowSquare;
    [SerializeField] private GameObject _healthBar;
    [SerializeField] private GameObject _skidLKuin;
    private Animator _skidStartAnimator;

    private void Start()
    {
        _skidStartAnimator = _skidStart.GetComponent<Animator>();

        _totalTime = _phase1Duration + _phase2Duration;

        StartCoroutine(LevelFlow());
    }

    private void Update()
    {
        // Update the level time
        int minutes = Mathf.FloorToInt(_totalTime / 60F);
        int seconds = Mathf.FloorToInt(_totalTime % 60F);
        _levelTime.text = $"Time: {minutes:00}:{seconds:00}";
        _totalTime -= Time.deltaTime * LocalTime.TimeScale;
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
        if (!_isPhase1 && !_isPhase2)
        {
            _levelTime.text = "00:00!";
            _levelPhase.text = "Boss Incoming!";
        }
    }

    private IEnumerator LevelFlow()
    {
        Debug.Log("Phase 1 started");
        LevelScoreManager.Instance.OnLevel = false;

        // Wait for the player to be ready
        yield return StartCoroutine(WaitForPausedSeconds(1.25f));
        LevelScoreManager.Instance.OnLevel = true;

        _isPhase1 = true;
        // 50% chance of spawning obstacles
        _obstacleCoroutine = StartCoroutine(SpawnObstacles(_obsProbabilityP1));
        // 3 follow enemies with 0.2s delay between each spawn
        _followEnemyCoroutine = StartCoroutine(SpawnFollowEnemies(_followEnemyDelayP1, _followEnemyAmountP1));
        // 1 pass enemy every random 2-5 seconds
        _passEnemyCoroutine = StartCoroutine(SpawnPassEnemies(_passEnemyTime));
        // 2 crazy enemies every random 4-8 seconds
        _crazyEnemyCoroutine = StartCoroutine(SpawnCrazyEnemies(_crazyEnemyDelayP1, _crazyEnemyAmountP1));
        // Wait for phase 1 to end
        yield return StartCoroutine(WaitForPausedSeconds(_phase1Duration));
        _isPhase1 = false;

        // Stop all coroutines from phase 1
        StopCoroutine(_obstacleCoroutine);
        StopCoroutine(_followEnemyCoroutine);
        StopCoroutine(_passEnemyCoroutine);
        StopCoroutine(_crazyEnemyCoroutine);

        Debug.Log("Phase 1 ended");

        Debug.Log("Phase 2 started");

        _isPhase2 = true;
        // 70% chance of spawning obstacles
        _obstacleCoroutine = StartCoroutine(SpawnObstacles(_obsProbabilityP2));
        // 4 follow enemies with 0.1s delay between each spawn
        _followEnemyCoroutine = StartCoroutine(SpawnFollowEnemies(_followEnemyDelayP2, _followEnemyAmountP2));
        // 1 pass enemy every random 2-5 seconds
        _passEnemyCoroutine = StartCoroutine(SpawnPassEnemies(_passEnemyTime));
        // 3 crazy enemies every random 4-8 seconds
        _crazyEnemyCoroutine = StartCoroutine(SpawnCrazyEnemies(_crazyEnemyDelayP2, _crazyEnemyAmountP2));
        // Wait for phase 2 to end
        yield return StartCoroutine(WaitForPausedSeconds(_phase2Duration));

        // Stop all coroutines from phase 2
        StopCoroutine(_obstacleCoroutine);
        StopCoroutine(_followEnemyCoroutine);
        StopCoroutine(_passEnemyCoroutine);
        StopCoroutine(_crazyEnemyCoroutine);

        Debug.Log("Phase 2 ended");

        // Wait until the spawners have no children
        while (_passEnemySpawner.gameObject.transform.childCount > 0)
        {
            bool allDisabled = true;
            foreach (Transform child in _passEnemySpawner.gameObject.transform)
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
        _water.StopScrolling();

        // Wait for the background to stop scrolling
        yield return StartCoroutine(WaitForPausedSeconds(1.5f));

        // Reset the player position
        _lillith.CanMove = false;
        _lillith.ReturnToStart();

        // Wait for the player to return to the start   
        yield return StartCoroutine(WaitForPausedSeconds(1.5f));

        // Skid L' Kuin presentation
        StartCoroutine(SkidPresentation());
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

            // Randomize between the two spawners
            var obstacleSpawner = Random.value < 0.5f ? _obstacleSpawnerV1 : _obstacleSpawnerV2;

            // Repeat the process every 1.5-3.5 seconds
            _obsTime = Random.Range(1.5f, 3.5f);
            obstacleSpawner.TrySpawn(probability);
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

            // Repeat the process every 2-5 seconds
            _passEnemyTime = Random.Range(2f, 5f);
            _passEnemySpawner.Spawn();
            yield return new WaitForSeconds(_passEnemyTime);
        }
    }

    private IEnumerator SpawnCrazyEnemies(float delay, int amount)
    {
        Debug.Log("Crazy enemy coroutine started");

        while (_isPhase1 || _isPhase2)
        {
            // Pause if LocalTime is not 1
            while (LocalTime.TimeScale != 1)
            {
                yield return null;
            }

            // Repeat the process every 4-8 seconds
            _crazyEnemyTime = Random.Range(4f, 8f);
            _crazyEnemySpawner.SpawnWave(delay, amount);
            yield return new WaitForSeconds(_crazyEnemyTime);
        }
    }

    private IEnumerator SkidPresentation()
    {
        // Get the object named "SoundMusicManager" in the scene
        GameObject musicManager = GameObject.Find("SoundMusicManager");
        AudioSource audioSource = musicManager.GetComponent<AudioSource>();

        // Fade out music
        float fadeOutDuration = 1.5f;
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

        // Arise from the water
        _skidStart.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        _skidStartAnimator.SetTrigger("idle");

        // Wait for the animation to finish
        yield return new WaitForSeconds(1.5f);

        // Smoothly increase the opacity of the splash art and the shadow square
        float opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime;
            _skidSplashArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Smoothly moves the splashart to a certain position while smoothing up the opacity of the text
        var splashartPosition = new Vector3(6.2f, 0f, 0f);
        elapsedTime = 0f;
        float duration = 3.25f;
        while (elapsedTime < duration)
        {
            _skidText.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, elapsedTime / duration);
            if (!LocalTime.IsPaused)
                _skidSplashArt.transform.position = Vector3.Lerp(_skidSplashArt.transform.position, splashartPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Wait for the animation to finish
        yield return new WaitForSeconds(0.25f);

        // Smoothly decrease the opacity of the splash art and the shadow square
        opacity = 1f;
        while (opacity > 0)
        {
            opacity -= Time.deltaTime;
            _skidSplashArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _skidText.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Let the player move again
        _lillith.CanMove = true;

        // Destroy the Skid start object
        Destroy(_skidStart);

        // Start the boss fight
        _levelTime.gameObject.SetActive(false);
        _healthBar.SetActive(true);
        _skidLKuin.SetActive(true);

        // Destroy itself
        Destroy(this);
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