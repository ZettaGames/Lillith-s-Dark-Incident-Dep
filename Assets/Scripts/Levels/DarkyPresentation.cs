using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkyPresentation : MonoBehaviour
{
    [Header("Lillith Controller")]
    [SerializeField] private LillithController _lillith;

    [Header("Darky N' Dark")]
    [SerializeField] private GameObject _darkyStart;
    [SerializeField] private GameObject _darkyLightSplashtArt;
    [SerializeField] private GameObject _darkyDarkSplashArt;
    [SerializeField] private GameObject _darkyText;
    [SerializeField] private GameObject _shadowSquare;
    [SerializeField] private GameObject _DarkyNDark;

    private void Awake()
    {
        _lillith.CanMove = false;
        LevelScoreManager.Instance.OnLevel = false;
    }

    private void Start()
    {
        GameManager.Instance.SetCurrentLevel(3);
        StartCoroutine(DarkyArrive());
    }

    private IEnumerator DarkyArrive()
    {
        yield return new WaitForSeconds(3.5f);

        // Smoothly increase the opacity of Darky splashart and the shadow square
        float opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale;
            _darkyLightSplashtArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Smoothly moves the splashart to a certain position while smoothing up the opacity of the text
        var splashartPosition = new Vector3(0f, 1f, 0f);
        var elapsedTime = 0f;
        var duration = 3.25f;
        while (elapsedTime < duration)
        {
            _darkyText.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, elapsedTime / duration);
            if (!LocalTime.IsPaused)
                _darkyLightSplashtArt.transform.position = Vector3.Lerp(_darkyLightSplashtArt.transform.position, splashartPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }

        // Flash the dark splashart
        opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * 10 * LocalTime.TimeScale;
            _darkyDarkSplashArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        while (opacity > 0)
        {
            opacity -= Time.deltaTime * 5 * LocalTime.TimeScale;
            _darkyDarkSplashArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Smoothly decrease the opacity of the floera sprite and the shadow square
        opacity = 1f;
        while (opacity > 0)
        {
            opacity -= Time.deltaTime * LocalTime.TimeScale;
            _darkyLightSplashtArt.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _darkyText.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            _shadowSquare.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, opacity / 2);
            yield return null;
        }

        // Let the player move again
        _lillith.CanMove = true;

        // Activate the boss
        _DarkyNDark.SetActive(true);

        // Destroy the Darky start object
        Destroy(_darkyStart);
    }
}
