using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(LillithController))]
public class LillithHealthManager : MonoBehaviour
{
    // Constant of the menu scene
    private const int MAIN_MENU = 2;

    // Constant for the animator
    private const string DEATH = "death";

    // Constants for the tags and layers
    private const string PLAYER = "Player";
	private const string ENEMY = "Enemy";
	private const string OBSTACLE = "Obstacle";
	private const string UNTAGGED = "Untagged";
	private const string DEFAULT = "Default";
	private const string INK = "Ink";
	private const string SCORE_DROP = "ScoreDrop";
    private const string STAR_DROP = "StarDrop";

    // Constants for the shake
    private const float SCREEN_SHAKE = 0.1f;
	private const float GAMEPAD_SHAKE = 0.5f;
	private const float SHAKE_DURATION = 0.25f;

	// Colors for invulnerability
	private readonly Color _traslucid = new Color(1, 1, 1, 0.75f);
	private readonly Color _solid = new Color(1, 1, 1, 1);

	// Health variables
	[Header("Health")]
	[SerializeField] private int currentStars;
	[SerializeField] private int _maxStars;
	[SerializeField] private Image[] _stars;
	public int CurrentStars
    {
        get => currentStars;
        set => currentStars = value;
    }
    private bool hasDied = false;

	// Sprites variables
	[Header("Sprites")]
	[SerializeField] private Sprite _fullStar;
	[SerializeField] private Sprite _emptyStar;

	// Damaging variables
	[Header("Damaging")]
	[SerializeField] private float _noControlTime;
	[SerializeField] private float _invincibilityTime;
	
	[Header("Knockback")]
	[SerializeField] private Vector2 _knockBackForce;

	[Header("SFX")]
	[SerializeField] private AudioClip _hitSound1;
    [SerializeField] private AudioClip _hitSound2;
	[SerializeField] private AudioClip _hitSound3;
	[SerializeField] private AudioClip _deathSound;

    // Components variables
    private Animator _animator;
	private Rigidbody2D _rigidbody;
	private LillithController _lillithController;
    private readonly List<ParticleCollisionEvent> _particleCollisions = new List<ParticleCollisionEvent>();
    private bool CanTakeDamage => !hasDied && currentStars > 0 &&
        LocalTime.TimeScale > 0f && gameObject.CompareTag(PLAYER);

	private void Start()
	{
		// Initialization of components
		_animator = GetComponent<Animator>();
		_rigidbody = GetComponent<Rigidbody2D>();
		_lillithController = GetComponent<LillithController>();
		
		// Set the stars to the maximum amount
		if (currentStars > _maxStars)
		{
			currentStars = _maxStars;
		}
	}

	private void Update()
	{
        // Limit the health to the maximum amount
        if (currentStars > _maxStars)
        {
            currentStars = _maxStars;
        }

        // Kill the player if there are no stars left
        if (currentStars <= 0 && !hasDied)
		{
			hasDied = true;
			StartCoroutine(Death());
		}

		for (int i = 0; i < _stars.Length; i++)
		{
			// Set the stars to full or empty
			if (i < currentStars)
			{
				_stars[i].sprite = _fullStar;
			}
			else
			{
				_stars[i].sprite = _emptyStar;
			}

			// Enable or disable the stars
			if (i < _maxStars)
			{
				_stars[i].enabled = true;
			}
			else
			{
				_stars[i].enabled = false;
			}
		}
	}

    #region damaging_methods
    public void TakeDamage()
	{
        if (!CanTakeDamage) return;
        // Play a random hit sound
        int random = Random.Range(0, 3);
        switch (random)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(_hitSound1, transform, 1f);
                break;
            case 1:
				// Change later
                SoundFXManager.Instance.PlaySoundFXClip(_hitSound3, transform, 1f);
                break;
            case 2:
                SoundFXManager.Instance.PlaySoundFXClip(_hitSound3, transform, 1f);
                break;
        }

        // Decrease the amount of stars
        currentStars--;

		// Apply the damaging effects
		StartCoroutine(NoControl());
		StartCoroutine(Invincibility());

		// Shake the screen
		ScreenShake.Instance.Shake(SCREEN_SHAKE, SHAKE_DURATION);
		// Shake the gamepad
		_lillithController.ShakeGamepad(GAMEPAD_SHAKE, GAMEPAD_SHAKE, SHAKE_DURATION);
	}

	private IEnumerator NoControl()
	{
		// Disable the player's movement
		_lillithController.CanMove = false;
		// Wait for a certain amount of time
		yield return WaitForPausedSeconds(_noControlTime);
        // Enable the player's movement
        _lillithController.CanMove = true;
	}

	private IEnumerator Invincibility()
	{
		// Make the player traslucid and untargetable
		GetComponent<SpriteRenderer>().color = _traslucid;
		gameObject.tag = UNTAGGED;
		gameObject.layer = LayerMask.NameToLayer(DEFAULT);

        // Elapsed time since the invincibility started
        float elapsedTime = 0.0f;

		// Loop until the invincibility time is over
		while (elapsedTime < _invincibilityTime)
		{
			// Only count the time if the game is not paused
			if (LocalTime.TimeScale != 0.0f)
			{
				elapsedTime += Time.deltaTime;
			}
			yield return null;
		}

		// Make the player solid and targetable
		GetComponent<SpriteRenderer>().color = _solid;
		gameObject.tag = PLAYER;
		gameObject.layer = LayerMask.NameToLayer(PLAYER);
	}
	
	private void KnockBack(Vector2 hitPoint)
	{
		// Move the player from the hit point
        KnockBackInDirection(_rigidbody.position - hitPoint);
	}

    private void KnockBackInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
        // Existing prefabs store negative values; strength must not reverse the impact.
        var strength = new Vector2(Mathf.Abs(_knockBackForce.x), Mathf.Abs(_knockBackForce.y));
        _rigidbody.velocity = Vector2.Scale(direction.normalized, strength);
    }

    private void KnockBackFromCollider(Collider2D other)
    {
        Vector2 point = other.ClosestPoint(_rigidbody.position);
        if ((point - _rigidbody.position).sqrMagnitude < 0.0001f)
            point = other.bounds.center;
        KnockBack(point);
    }

	private IEnumerator Death()
	{
        // Make the player solid
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);

        // Stop the game
        LocalTime.TimeScale = 0.0f;

        // Play the death sound
        SoundFXManager.Instance.PlaySoundFXClip(_deathSound, transform, 1f);

        // Prevent more collisions
        transform.tag = UNTAGGED;
        yield return new WaitForSeconds(0.25f);

        // Play the death animation
        _animator.SetTrigger(DEATH);
		yield return new WaitForSeconds(1.5f);

        // Load the main menu and resume the game
        SceneTransitionManager.Instance.LoadLevel(MAIN_MENU);
        yield return new WaitForSeconds(1.2f);
		LocalTime.TimeScale = 1.0f;
	}
    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the player collided with an enemy
        if ((collision.collider.CompareTag(ENEMY) || collision.collider.CompareTag(OBSTACLE)) && CanTakeDamage)
        {
            TakeDamage();
            if (collision.contactCount > 0)
                KnockBackInDirection(collision.GetContact(0).normal);
            else
                KnockBackFromCollider(collision.collider);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (LocalTime.TimeScale <= 0f || hasDied) return;
        // Check if the player collided with an enemy
        if ((other.CompareTag(ENEMY) || other.CompareTag(OBSTACLE)) && CanTakeDamage)
        {
            // Update the score
            LevelScoreManager.Instance.HitPenalty();

            // Apply the damaging effects
            TakeDamage();
            KnockBackFromCollider(other);
        }

        // Check if the player collided with a score drop
        if (other.CompareTag(SCORE_DROP))
        {
            // Update the score
            LevelScoreManager.Instance.ScoreBonus();
            Destroy(other.gameObject);
        }

        // Check if the player collided with a star drop
        if (other.CompareTag(STAR_DROP))
        {
            if (currentStars < _maxStars)
            {
                // Increase the amount of stars
                currentStars++;
            }
            else
            {
                // Update the score
                LevelScoreManager.Instance.ScoreBonus();
            }
            Destroy(other.gameObject);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (!CanTakeDamage) return;
        var particles = other.GetComponent<ParticleSystem>();
        int collisionCount = particles != null
            ? particles.GetCollisionEvents(gameObject, _particleCollisions) : 0;

		if (other.CompareTag(INK))
		{
            // Unity batches particle contacts; show one spot for each ink ball.
            for (int i = 0; i < Mathf.Max(1, collisionCount); i++)
                InkManager.Instance.ShowInkSpot();
        }
		else
		{
            // Update the score
            LevelScoreManager.Instance.HitPenalty();

            // Apply the damaging effects
            TakeDamage();
            if (collisionCount > 0)
            {
                var impact = _particleCollisions[0];
                if (impact.velocity.sqrMagnitude > 0.0001f)
                    KnockBackInDirection(impact.velocity);
                else
                    KnockBack(impact.intersection);
            }
			else KnockBack(other.transform.position);
		}
    }

    // Do constant damage if the player is inside the collider
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(OBSTACLE) && CanTakeDamage)
        {
            TakeDamage();
            KnockBackFromCollider(other);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(OBSTACLE) && CanTakeDamage)
        {
            TakeDamage();
            if (collision.contactCount > 0)
                KnockBackInDirection(collision.GetContact(0).normal);
            else
                KnockBackFromCollider(collision.collider);
        }
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
