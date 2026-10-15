using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
	// ! Singleton instance of the Screen Shake
	public static ScreenShake Instance { get; private set; }

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

	// Call the screen based on the duration and magnitude
	public void Shake(float duration, float magnitude)
	{
		StartCoroutine(ShakeCoroutine(duration, magnitude));
	}

	private IEnumerator ShakeCoroutine(float duration, float magnitude)
	{
		// Save the original position of the camera
		Vector3 originalPosition = transform.localPosition;
		float elapsed = 0.0f;

		// Shake the camera for the duration time
		while (elapsed < duration)
		{
            if (LocalTime.IsPaused)
            {
                yield return null;
                continue;
            }
			// Calculate the new position of the camera based on the magnitude
			float x = Random.Range(-1.0f, 1.0f) * magnitude;
			float y = Random.Range(-1.0f, 1.0f) * magnitude;

			// Set the new position of the camera
			transform.localPosition = new Vector3(x, y, originalPosition.z);

			// Increase the elapsed time
			elapsed += Time.deltaTime;

			yield return null;
		}

		// Reset the camera position to the original position
		transform.localPosition = originalPosition;
	}
}
