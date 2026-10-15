using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuActionsManager : MonoBehaviour
{
	// Constants
	private const int NewGameIndex = 3;
	private const string ExitSceneName = "ExitScene";
	private const string CreditsSceneName = "CreditsScene";

	[SerializeField] private TMP_Text _scorePrefText;

    private void Update()
    {
        _scorePrefText.text = $"Your current high score is: {PlayerPrefs.GetFloat("TotalScore", 0):000000000}";
    }

    public void NewGameYes()
	{
		if (SceneTransitionManager.Instance.IsLoading) return;
		GameManager.Instance.ResetRun();
		StartCoroutine(StartNewGame());
	}
	
	public void ExitYes()
	{
		StartCoroutine(ExitTransition());
	}

    public void Credits()
    {
        EventSystem.current.SetSelectedGameObject(null);
        SceneTransitionManager.Instance.LoadLevel(CreditsSceneName);
    }

    private IEnumerator StartNewGame()
	{
		EventSystem.current.SetSelectedGameObject(null);
		SceneTransitionManager.Instance.LoadLevel(NewGameIndex);
        yield return null;
	}
	
	private IEnumerator ExitTransition()
	{
		EventSystem.current.SetSelectedGameObject(null);
        SceneTransitionManager.Instance.LoadLevel(ExitSceneName);
        yield return null;
	}
}
