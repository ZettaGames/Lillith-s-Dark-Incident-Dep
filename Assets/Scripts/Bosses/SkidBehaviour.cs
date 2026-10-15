using System.Collections;
using UnityEngine;

public class SkidBehaviour : BossGeneric
{
    // CONSTANTS
    // States
    private const string HELL = "hell";
    private const string RETURN = "return";
    private const string DEATH = "death";

    // Scenes
    private const string SCORE_SCREEN = "ScoreScreen";

    // Tags
    private const string INK = "Ink";
    private const string UNTAGGED = "Untagged";

    // Skid Phrases
    private string[] _phrases = new string[]
    {
        "Lil shrimp, you dare challenge the Kuin?",
        "Time to wave goodbye!",
        "Sink in despair! Feel the squeeze!",
        "Say hello to Davy Jones for me!",
        "Let’s ink out your little mistake!",
        "You dare me? What a damp squid!",
    };

    [Header("Attack Probabilities")]
    [SerializeField] private float[] _attackProbabilies = new float[3];

    [Header("Bullet Sprites")]
    [SerializeField] private Sprite _rockBullet;
    [SerializeField] private Sprite _inkBullet;

    [Header("Tentacles")]
    [SerializeField] private GameObject _tentacleTrap;
    [SerializeField] private GameObject _tentacleShoot;
    [SerializeField] private GameObject _rockProjectile;

    [Header("Colliders")]
    [SerializeField] private Collider2D _wanderCollider;
    [SerializeField] private Collider2D _tentacleTrapCollider;
    [SerializeField] private Collider2D _tentacleShootCollider;

    private Vector3 _startPosition = new Vector3(0, 3, 0);
    private Vector3 _centerPosition = Vector3.zero;

    private bool _canWander = true;
    private bool _isTraping;
    private bool _isShooting;
    private bool _isHell;

    // Components
    private BulletSystem _bulletSystem;
    private Animator _animator;

    private SkidAttackType _currentAttackType;
    private int _previousAttackType = (int)SkidAttackType.TentacleShoot;
    private enum SkidAttackType
    {
        TentacleTrap,
        TentacleShoot,
        Hell
    }

    private void Start()
    {
        GameManager.Instance.SetCurrentLevel(2);

        _animator = GetComponent<Animator>();
        _bulletSystem = GetComponent<BulletSystem>();
        _audioSource = _musicPlayer.GetComponent<AudioSource>();

        // Start the music
        _audioSource.clip = _music;
        _audioSource.Play();

        _currentHealth = _maxHealth;

        StartCoroutine(ChangePhrase());
        StartCoroutine(Wander());

        _timer = _timeBetweenAttacks;
    }

    private void Update()
    {
        if (LocalTime.IsPaused) return;
        // Boss control
        _healthBar.fillAmount = (float)_currentHealth / _maxHealth;
        _timer += Time.deltaTime * LocalTime.TimeScale;

        // Change the attack if the boss is not trapping, shooting or in hell
        if (!_isTraping && !_isShooting && !_isHell)
        {
            // Stop all current bullet patterns
            _bulletSystem.StopSpawner();

            // Select a new attack
            _currentAttackType = (SkidAttackType)Choose(_attackProbabilies, _previousAttackType);
            _previousAttackType = (int)_currentAttackType;
            switch (_currentAttackType)
            {
                case SkidAttackType.TentacleTrap:
                    _isTraping = true;
                    StartCoroutine(TentacleTrap());
                    break;
                case SkidAttackType.TentacleShoot:
                    _isShooting = true;
                    StartCoroutine(TentacleShoot());
                    break;
                case SkidAttackType.Hell:
                    _isHell = true;
                    StartCoroutine(Hell());
                    break;
            }
        }
    }

    private IEnumerator Wander()
    {
        float minDistance = 2.0f;

        while (_canWander)
        {
            Vector3 randomPosition;
            do
            {
                // Choose a random position inside the wander collider
                randomPosition = new Vector3(
                    Random.Range(_wanderCollider.bounds.min.x, _wanderCollider.bounds.max.x),
                    Random.Range(_wanderCollider.bounds.min.y, _wanderCollider.bounds.max.y),
                    transform.position.z
                );
            } while (Vector3.Distance(transform.position, randomPosition) < minDistance);

            // Move to the random position
            MoveToDestination(randomPosition, 1.5f);

            // Wait for the movement to end
            yield return WaitForPausedSeconds(3f);
        }
    }

    private IEnumerator TentacleTrap()
    {
        // Start the ink bullet pattern
        SetBulletInk();
        _bulletSystem.StartSpawner();

        // Spawn the tentacle trap in a random position and repeat the process
        for (int i = 0; i < 6; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(_tentacleTrapCollider.bounds.min.x, _tentacleTrapCollider.bounds.max.x),
                Random.Range(_tentacleTrapCollider.bounds.min.y, _tentacleTrapCollider.bounds.max.y),
                transform.position.z
            );
            var tentacle = Instantiate(_tentacleTrap, randomPosition, Quaternion.identity);
            yield return WaitForPausedSeconds(1.25f);

            // Start the tentacle trap bullet pattern
            tentacle.GetComponent<BulletSystem>().StartSpawner();
            yield return WaitForPausedSeconds(2.75f);
            Destroy(tentacle);
        }

        _isTraping = false;
    }

    private IEnumerator TentacleShoot()
    {
        // Start the ink bullet pattern
        SetBulletInk();
        _bulletSystem.StartSpawner();

        // Spawn the tentacle shoot in a random position and repeat the process
        for (int i = 0; i < 6; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(_tentacleShootCollider.bounds.min.x, _tentacleShootCollider.bounds.max.x),
                Random.Range(_tentacleShootCollider.bounds.min.y, _tentacleShootCollider.bounds.max.y),
                transform.position.z
            );
            var tentacle = Instantiate(_tentacleShoot, randomPosition, Quaternion.identity);
            // Randomly flip the z scale of the tentacle
            if (Random.Range(0, 2) == 0)
            {
                tentacle.transform.localScale = new Vector3(tentacle.transform.localScale.x, tentacle.transform.localScale.y, -tentacle.transform.localScale.z);
            }
            yield return WaitForPausedSeconds(0.75f);

            // Instantiate the rock projectile
            Instantiate(_rockProjectile, randomPosition, Quaternion.identity);
            yield return WaitForPausedSeconds(0.75f);
            Destroy(tentacle);
        }
        _isShooting = false;
    }

    private IEnumerator Hell()
    {
        // Stop the wander state and wait for the boss to stop moving
        _canWander = false;
        StopCoroutine(Wander());
        yield return WaitForPausedSeconds(2f);

        // Move the boss to the center
        MoveToDestination(_centerPosition, 0.75f);

        // Start the hell animation
        _animator.SetTrigger(HELL);
        yield return WaitForPausedSeconds(1.75f);

        // Start the hell bullet pattern
        SetBulletHell();
        _bulletSystem.StartSpawner();
        yield return WaitForPausedSeconds(12f);

        // Move the boss to the start position
        _bulletSystem.StopSpawner();
        _animator.SetTrigger(RETURN);
        yield return WaitForPausedSeconds(1.5f);

        // Return to the wander state
        _bulletSystem.StopSpawner();
        MoveToDestination(_startPosition, 0.25f);
        _canWander = true;
        _isHell = false;
        StartCoroutine(Wander());
    }

    protected override void Death()
    {
        // Stop the game
        LocalTime.TimeScale = 0.0f;
        StopAllCoroutines();

        // Play the death animation
        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
    {
        // Make the boss solid
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);

        // Play the death animation
        _animator.SetTrigger(DEATH);
        yield return new WaitForSeconds(6.5f);

        // Save the score
        LevelScoreManager.Instance.SaveScore();

        // Load the main menu and resume the game
        yield return new WaitForSeconds(1.2f);
        SceneTransitionManager.Instance.LoadLevel(SCORE_SCREEN);
    }

    private void SetBulletInk()
    {
        _bulletSystem.CurrentPattern = BulletSystem.PatternType.Circular;
        _bulletSystem.BulletsTag = INK;
        _bulletSystem.SpawnerName = "SkidInk";
        _bulletSystem.BulletSprite = _inkBullet;
        _bulletSystem.BulletSpeed = 6f;
        _bulletSystem.BulletSize = 0.4f;
        _bulletSystem.NumberOfColumns = 35;
        _bulletSystem.FireRate = 1.5f;
        _bulletSystem.SpinSpeed = 0;
    }

    private void SetBulletHell()
    {
        _bulletSystem.CurrentPattern = BulletSystem.PatternType.MultiSpiral;
        _bulletSystem.BulletsTag = UNTAGGED;
        _bulletSystem.SpawnerName = "SkidHell";
        _bulletSystem.BulletSprite = _rockBullet;
        _bulletSystem.BulletSpeed = 4f;
        _bulletSystem.BulletSize = 0.4f;
        _bulletSystem.NumberOfColumns = 10;
        _bulletSystem.FireRate = 0.2f;
        _bulletSystem.SpinSpeed = 50;
    }

    private IEnumerator ChangePhrase()
    {
        while (true)
        {
            _stateText.text = _phrases[Random.Range(0, _phrases.Length)];
            Debug.Log("Phrase changed to " + _stateText.text);
            yield return WaitForPausedSeconds(15f);
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