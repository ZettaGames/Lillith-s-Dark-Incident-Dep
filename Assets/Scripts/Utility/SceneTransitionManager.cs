using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    // CONSTANTS
    // Level loading
    private const float InitialDelay = 0.25f;
    private const float TransitionDelay = 1f;

    // Main menu transition
    private const int BlackSceneIndex = 1;
    private const int MainMenuIndex = 2;
    private const float MainMenuInitialDelay = 2.0f;

    // Triggers
    private const string StartTransitionTrigger = "StartTransition";

    // Componentes
    [Header("Components")]
    [SerializeField] private CanvasGroup _coverImage;
    [SerializeField] private Animator _transitionAnimator;

    // Singleton instance
    public static SceneTransitionManager Instance { get; private set; }
    public bool IsLoading { get; private set; }

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
        if (IsOnBlackScene())
        {
            IsLoading = true;
            StartCoroutine(LoadMainMenu());
        }
    }

    #region Level Loading
    public void LoadLevel(int index)
    {
        if (IsLoading) return;
        IsLoading = true;
        LocalTime.SetPaused(false);
        StartCoroutine(FadeOutMusic());
        StartCoroutine(Load(index));
    }

    public void LoadLevel(string name)
    {
        if (IsLoading) return;
        IsLoading = true;
        LocalTime.SetPaused(false);
        StartCoroutine(FadeOutMusic());
        StartCoroutine(Load(name));
    }

    private IEnumerator Load(int index)
    {
        yield return new WaitForSeconds(InitialDelay);

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(index);
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Start the transition animation
        _transitionAnimator.SetTrigger(StartTransitionTrigger);
        yield return new WaitForSeconds(TransitionDelay);

        // Allow the scene to be activated
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;
        IsLoading = false;
    }

    private IEnumerator Load(string name)
    {
        yield return new WaitForSeconds(InitialDelay);

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name);
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Start the transition animation
        _transitionAnimator.SetTrigger(StartTransitionTrigger);
        yield return new WaitForSeconds(TransitionDelay);

        // Allow the scene to be activated
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;
        IsLoading = false;
    }
    #endregion

    private IEnumerator LoadMainMenu()
    {
        // De-activate the cover image after the delay
        yield return new WaitForSeconds(MainMenuInitialDelay);
        _coverImage.gameObject.SetActive(false);

        // Load the main menu scene asynchronously
        yield return Load(MainMenuIndex);
    }

    private bool IsOnBlackScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.buildIndex == BlackSceneIndex;
    }

    private IEnumerator FadeOutMusic()
    {
        // Get the object named "SoundMusicManager" in the scene
        GameObject musicManager = GameObject.Find("SoundMusicManager");

        if (musicManager != null)
        {
            // Get the AudioSource component
            AudioSource audioSource = musicManager.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // Fade out the music over 1 second
                float startVolume = audioSource.volume;
                float fadeDuration = 1f;
                float elapsedTime = 0f;
                while (elapsedTime < fadeDuration)
                {
                    audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                // Ensure volume is set to 0 at the end
                audioSource.volume = 0f;
            }
        }
    }
}
