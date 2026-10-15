using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput), typeof(Animator))]
public class LillithController : MonoBehaviour
{
	#region variables
	// Constant for the animator
	private const string SPEED = "xSpeed";
	private const string BREAK = "Break";

	// Constants for the input actions
	private const string MOVE = "Move";
	private const string SHOOT = "Shoot";
	private const string SUPER = "Super";
	private const string SHIELD = "Shield";

	[Header("Movement")]
	// Movement variables
	[SerializeField] private float _moveSpeed;
	[SerializeField] private float _smoothMove;
	public bool CanMove { get; set; } = true;

	// Vector variables
	private Vector2 _input;
	private Vector2 _fixedInput;
	private Vector2 _velocity = Vector2.zero;

	[Header("Shooting")]
	[SerializeField, Range(0.01f, 0.05f)] private float _shootCooldown;
	private float _shootTimer;

	[Header("Super Attack")]
	[SerializeField] private GameObject _superAttack;
	[SerializeField] private float _superCooldown;
	private float _superTimer;

	[Header("Super UI")]
	[SerializeField] private TMP_Text _superText;
	[SerializeField] private Image _superIcon;

	[Header("Shield")]
	[SerializeField] private GameObject _shield;
	[SerializeField] private float _shieldDuration;
	[SerializeField] private float _shieldCooldown;
	private float _shieldTimer;

	[Header("Shield UI")]
	[SerializeField] private TMP_Text _shieldText;
	[SerializeField] private Image _shieldIcon;

	[Header("SFX")]
    [SerializeField] private AudioClip _super1;
    [SerializeField] private AudioClip _super2;
    [SerializeField] private AudioClip _super3;
    [SerializeField] private AudioClip _bubble1;
    [SerializeField] private AudioClip _bubble2;
    [SerializeField] private AudioClip _bubble3;

    // Unity components
    private Rigidbody2D _rigidbody;
	private PlayerInput _playerInput;
	private Animator _animator;
    private Gamepad _rumblingGamepad;
    private Coroutine _rumbleCoroutine;
	#endregion

	#region unity_methods
	private void Start()
	{
		// Initialization of components
		_rigidbody = GetComponent<Rigidbody2D>();
		_playerInput = GetComponent<PlayerInput>();
		_animator = GetComponent<Animator>();

		// Hability cooldowns
		_shootTimer = _shootCooldown;
		_superTimer = _superCooldown;
		_shieldTimer = _shieldCooldown;
	}

	private void Update()
	{
		// Update the animator
		_animator.SetFloat(SPEED, _input.x);

		// Update the UI for Super Attack
		if (_superTimer >= _superCooldown)
		{
			_superText.text = ": Ready";
			_superIcon.color = Color.white;
		}
		else
		{
			_superText.text = $": {(int)(_superCooldown - _superTimer)} s";
			_superIcon.color = Color.grey;
		}

		// Update the UI for Shield
		if (_shieldTimer >= _shieldCooldown)
		{
			_shieldText.text = ": Ready";
			_shieldIcon.color = Color.white;
		}
		else
		{
			_shieldText.text = $": {(int)(_shieldCooldown - _shieldTimer)} s";
			_shieldIcon.color = Color.grey;
		}

		if (LocalTime.TimeScale > 0.0f && CanMove)
		{
			// Shoot
			_shootTimer += Time.deltaTime;
			Shoot();

			// Read input
			ReadInput();

			// Super shoot
			_superTimer += Time.deltaTime;
			SuperShoot();

			// Shield
			_shieldTimer += Time.deltaTime;
			BubbleShield();
		}
	}

	private void FixedUpdate()
	{
		// Move the player
		if (CanMove)
		{
			Move();
		}
	}
	#endregion

	private void ReadInput()
	{
		// Read input from the player
		_input = _playerInput.actions[MOVE].ReadValue<Vector2>().normalized;
	}

	#region lillith_actions
	private void Move()
	{
		// Smooth the input
		_fixedInput = Vector2.SmoothDamp(_fixedInput, _input, ref _velocity, _smoothMove);

		// Move the player
		_rigidbody.velocity = _fixedInput * _moveSpeed * LocalTime.TimeScale;
	}

	private void Shoot()
	{
		if (_playerInput.actions[SHOOT].IsPressed() && _shootTimer >= _shootCooldown)
		{
			// Reset the timer
			_shootTimer = 0f;

			// Shoot a bullet from the pool manager
			var bullet = LillithPoolManager.Instance.ShootBullet();
			bullet.transform.position = transform.position;
			bullet.transform.rotation = transform.rotation;
		}
	}

	private void SuperShoot()
	{
		if (_playerInput.actions[SUPER].IsPressed() && _superTimer >= _superCooldown)
		{
			// Reset the timer
			_superTimer = 0f;

            // Play a random super attack sound
            var random = Random.Range(0, 3);
			switch(random)
            {
                case 0:
                    SoundFXManager.Instance.PlaySoundFXClip(_super1, transform, 1f);
                    break;
                case 1:
                    SoundFXManager.Instance.PlaySoundFXClip(_super2, transform, 1f);
                    break;
                case 2:
                    SoundFXManager.Instance.PlaySoundFXClip(_super3, transform, 1f);
                    break;
            }

            // Launch the player backwards and shoot the super attack
            StartCoroutine(Super());
		}
	}

	private void BubbleShield()
	{
		if (_playerInput.actions[SHIELD].IsPressed() && _shieldTimer >= _shieldCooldown)
		{
			// Reset the timer
			_shieldTimer = 0f;

            // Play a random bubble sound
            var random = Random.Range(0, 3);
            switch (random)
            {
                case 0:
                    SoundFXManager.Instance.PlaySoundFXClip(_bubble1, transform, 1f);
                    break;
                case 1:
                    SoundFXManager.Instance.PlaySoundFXClip(_bubble2, transform, 1f);
                    break;
                case 2:
                    SoundFXManager.Instance.PlaySoundFXClip(_bubble3, transform, 1f);
                    break;
            }

            // Create the shield
            StartCoroutine(Bubble());
		}
	}

	private IEnumerator Super()
	{
		CanMove = false;
		_rigidbody.velocity = Vector2.down * 2.5f;
		ShakeGamepad(0.25f, 0.25f, 0.25f);
		Instantiate(_superAttack, transform.position, Quaternion.identity);
		yield return new WaitForSeconds(0.3f);
		CanMove = true;
	}

	private IEnumerator Bubble()
	{
		CanMove = false;
		_rigidbody.velocity = Vector2.zero;
		var bubble = Instantiate(_shield, transform.position, Quaternion.identity);
		bubble.transform.parent = transform;
		yield return new WaitForSeconds(0.3f);
		CanMove = true;
		GetComponentInChildren<LillithShield>().Break(_shieldDuration, BREAK);
	}
	#endregion

	// Return the player to the initial position
	public void ReturnToStart()
	{
		StartCoroutine(ReturnPlayer());
	}

    private IEnumerator ReturnPlayer()
    {
        // Start position
        var startPosition = new Vector3(0f, -4f, 0f);

        // Stop the player
        _rigidbody.velocity = Vector2.zero;

        // Smoothly return the player to the initial position
        while (Vector2.Distance(transform.position, startPosition) > 0.1f)
        {
            if (LocalTime.TimeScale > 0f)
            {
                transform.position = Vector2.Lerp(transform.position, startPosition,
                    1f - Mathf.Exp(-3f * Time.deltaTime * LocalTime.TimeScale));
            }
            yield return null;
        }
    }

    #region gamepad_shake
    public void ShakeGamepad(float lowFreqIntensity, float highFreqIntensity, float duration)
	{
        StopGamepadShake();
        if (!isActiveAndEnabled || LocalTime.IsPaused || Gamepad.current == null) return;
        _rumblingGamepad = Gamepad.current;
        _rumbleCoroutine = StartCoroutine(GamepadShake(lowFreqIntensity, highFreqIntensity, duration));
	}
	
	private IEnumerator GamepadShake(float lowFreqIntensity, float highFreqIntensity, float duration)
	{
        _rumblingGamepad.SetMotorSpeeds(lowFreqIntensity, highFreqIntensity);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
        if (_rumblingGamepad != null && _rumblingGamepad.added)
            _rumblingGamepad.SetMotorSpeeds(0f, 0f);
        _rumblingGamepad = null;
        _rumbleCoroutine = null;
	}

    public void StopGamepadShake()
    {
        if (_rumbleCoroutine != null) StopCoroutine(_rumbleCoroutine);
        if (_rumblingGamepad != null && _rumblingGamepad.added)
            _rumblingGamepad.SetMotorSpeeds(0f, 0f);
        _rumbleCoroutine = null;
        _rumblingGamepad = null;
    }

    private void OnDisable() => StopGamepadShake();
    private void OnDestroy() => StopGamepadShake();
    private void OnApplicationQuit() => StopGamepadShake();
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) StopGamepadShake();
    }

	public void ResetCooldowns()
	{
        _superTimer = _superCooldown;
        _shieldTimer = _shieldCooldown;
    }

	public void DestroyShields()
	{
		var shield = FindObjectOfType<LillithShield>();
        if (shield != null)
        {
            Destroy(shield.gameObject);
        }
    }
	#endregion
}
