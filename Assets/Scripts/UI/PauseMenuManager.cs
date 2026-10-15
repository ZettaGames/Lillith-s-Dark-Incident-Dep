using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    // ! Constants
    private const string PAUSE_ACTION = "Pause";
    private const float BUTTON_DELAY = 0.25f;
    private const float SPAM_DELAY = 0.05f;

    private const string GO_UP = "GoUp";
    private const string GO_DOWN = "GoDown";

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button _resumeButton;

    [Header("Components")]
    [SerializeField] private InputActionAsset _actionAsset;
    [SerializeField] private Animator _pauseAnimator;
    private InputAction _pauseAction;
    private bool _transitioning;

    private void Start()
    {
        foreach (var animator in _pauseAnimator.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        // Initialization of the pause action
        _pauseAction = _actionAsset.FindActionMap("Player").FindAction(PAUSE_ACTION);
        if (_pauseAction != null)
        {
            _pauseAction.Enable();
        }
        else
        {
            Debug.LogError("Pause action not found in the InputActionAsset.");
        }
    }

    private void Update()
    {
        if (_pauseAction != null && _pauseAction.triggered && !_transitioning &&
            !SceneTransitionManager.Instance.IsLoading)
        {
            if (LocalTime.IsPaused)
            {
                StartCoroutine(Resume());
            }
            else if (LocalTime.TimeScale > 0f)
            {
                StartCoroutine(Pause());
            }
        }
    }

    public void ResumeButton()
    {
        if (_transitioning || !LocalTime.IsPaused) return;
        StartCoroutine(Resume());
    }

    public void MainMenuButton()
    {
        if (_transitioning || SceneTransitionManager.Instance.IsLoading) return;
        LocalTime.SetPaused(false);
        EventSystem.current.SetSelectedGameObject(null);
        SceneTransitionManager.Instance.LoadLevel(2);
        LocalTime.TimeScale = 1;
    }

    private IEnumerator Pause()
    {
        // Pause the game
        _transitioning = true;
        LocalTime.SetPaused(true);
        foreach (var player in FindObjectsOfType<LillithController>())
        {
            player.StopGamepadShake();
        }

        // Prevent button spam
        _actionAsset.Disable();
        // Animation of the pause menu
        _pauseAnimator.SetTrigger(GO_UP);
        // Wait to prevent button spam
        yield return new WaitForSecondsRealtime(SPAM_DELAY);
        EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
        _actionAsset.Enable();
        _transitioning = false;
    }

    private IEnumerator Resume()
    {
        _transitioning = true;
        // Prevent button spam
        _actionAsset.Disable();
        // Wait to play button animation
        yield return new WaitForSecondsRealtime(BUTTON_DELAY);
        // Animation of the pause menu
        _pauseAnimator.SetTrigger(GO_DOWN);
        // Wait to prevent button spam
        yield return new WaitForSecondsRealtime(SPAM_DELAY);
        EventSystem.current.SetSelectedGameObject(null);
        _actionAsset.Enable();
        yield return new WaitForSecondsRealtime(BUTTON_DELAY);

        // Resume the game
        LocalTime.SetPaused(false);
        _transitioning = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        LocalTime.SetPaused(false);
        if (_transitioning && _actionAsset != null) _actionAsset.Enable();
        _transitioning = false;
    }
}
