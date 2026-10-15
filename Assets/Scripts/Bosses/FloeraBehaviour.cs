using System.Collections;
using UnityEngine;

public class FloeraBehaviour : BossGeneric
{
    // Constants
    private const string LEFT_SIDE = "left";
    private const string RIGHT_SIDE = "right";
    private const string HIDE = "hide";
    private const string TRAP = "trap";
    private const string ARISE = "arise";
    private const string HELL = "hell";
    private const string DEATH = "death";
    private const string SCORE_SCREEN = "ScoreScreen";

    // Floera Phrases
    private string[] _phrases = new string[]
    {
        "Falling already? It's not autumn yet!",
        "Say goodbye, sapling!",
        "You're not going to leaf this place alive!",
        "Be afraid, be cherry afraid!",
        "Prepare to be rooted in fear!",
        "You barked up the wrong tree!",
    };

    [Header("Floera Settings")]
    [SerializeField] private float[] _attacksPropabilities = new float[3];

    private Vector3 _leftside = new Vector3(-3, 0, 0);
    private Vector3 _rightSide = new Vector3(3, 0, 0);

    private int _timesToMove;
    private string _sideString;
    private Vector3 _sideVector;

    [SerializeField] private BoxCollider2D _trapArea;
    private Vector3[] _trapPositions;


    private bool _isMoving;
    private bool _isRooting;
    private bool _isHell;

    // Components
    private BulletSystem _bulletSystem;
    private Animator _animator;

    private FloeraAttackType _currentAttack;
    private int _lastAttack = (int)FloeraAttackType.RootAttack;
    private enum FloeraAttackType
    {
        MoveAttack,
        RootAttack,
        HellAttack
    }

    private void Start()
    {
        GameManager.Instance.SetCurrentLevel(1);

        _animator = GetComponent<Animator>();
        _bulletSystem = GetComponent<BulletSystem>();
        _audioSource = _musicPlayer.GetComponent<AudioSource>();

        // Start the music
        _audioSource.clip = _music;
        _audioSource.Play();

        _currentHealth = _maxHealth;
        Debug.Log($"Max Health: {_maxHealth}, Current Health: {_currentHealth}");

        StartCoroutine(ChangePhrase());

        _timer = _timeBetweenAttacks;
    }

    private void Update()
    {
        if (LocalTime.IsPaused) return;
        _healthBar.fillAmount = (float)_currentHealth / _maxHealth;

        _timer += Time.deltaTime * LocalTime.TimeScale;

        // Change the attack if the boss is not moving, rooting or in hell
        if (!_isMoving && !_isRooting && !_isHell)
        {
            _currentAttack = (FloeraAttackType)Choose(_attacksPropabilities, _lastAttack);
            _lastAttack = (int)_currentAttack;
            Debug.Log("Current Attack: " + _currentAttack);
            switch (_currentAttack)
            {
                case FloeraAttackType.MoveAttack:
                    // Stop any bullet system that is running
                    _bulletSystem.StopSpawner();

                    // Set the times the boss will move
                    _timesToMove = Random.Range(1, 3);

                    // Set the bullet system
                    SetBulletMove();
                    _bulletSystem.StartSpawner();

                    // Start the attack
                    MoveAttack();
                    break;
                case FloeraAttackType.RootAttack:
                    // Stop any bullet system that is running
                    _bulletSystem.StopSpawner();

                    // Set the bullet system
                    SetBulletTrap();

                    // Start the attack
                    RootAttack();
                    break;
                case FloeraAttackType.HellAttack:
                    // Stop any bullet system that is running
                    _bulletSystem.StopSpawner();

                    // Set the bullet system
                    SetBulletHell();

                    // Start the attack
                    HellAttack();
                    break;
            }
        }
    }

    #region Move Attack
    private void MoveAttack()
    {
        Debug.Log("Deciding where to move");

        _isMoving = true;

        // Choose a random side to move
        _sideString = Random.Range(0, 2) == 0 ? LEFT_SIDE : RIGHT_SIDE;
        _sideVector = _sideString == LEFT_SIDE ? _leftside : _rightSide;

        Debug.Log("Moving to " + _sideString);

        StartCoroutine(MoveAndReturn(_sideString, _sideVector));
    }

    private IEnumerator MoveAndReturn(string trigger, Vector3 position)
    {
        // Obtain the opposite side
        string oppositeSide = trigger == LEFT_SIDE ? RIGHT_SIDE : LEFT_SIDE;
        Vector3 oppositePosition = trigger == LEFT_SIDE ? _rightSide : _leftside;

        // Move to the chosen side
        _animator.SetTrigger(trigger);
        yield return WaitForPausedSeconds(1.25f);
        MoveToDestination(transform.position + position, 0.25f);

        // Stay in the side for a while
        yield return WaitForPausedSeconds(2.25f);

        // Return to the center
        _animator.SetTrigger(oppositeSide);
        yield return WaitForPausedSeconds(1.25f);
        MoveToDestination(transform.position + oppositePosition, 0.25f);

        // Repeat the process
        _timesToMove--;
        if (_timesToMove > 0)
        {
            yield return WaitForPausedSeconds(2.25f);
            MoveAttack();
        }
        else
        {
            yield return WaitForPausedSeconds(2.25f);
            _isMoving = false;
        }
    }
    #endregion

    #region Root Attack
    private void RootAttack()
    {
        _isRooting = true;

        // Set six random positions for the trap based on the trap area
        _trapPositions = new Vector3[5];
        _trapPositions[0] = new Vector3(Random.Range(_trapArea.bounds.min.x, _trapArea.bounds.max.x), Random.Range(_trapArea.bounds.min.y, _trapArea.bounds.max.y), 0);
        _trapPositions[1] = new Vector3(Random.Range(_trapArea.bounds.min.x, _trapArea.bounds.max.x), Random.Range(_trapArea.bounds.min.y, _trapArea.bounds.max.y), 0);
        _trapPositions[2] = new Vector3(Random.Range(_trapArea.bounds.min.x, _trapArea.bounds.max.x), Random.Range(_trapArea.bounds.min.y, _trapArea.bounds.max.y), 0);
        _trapPositions[3] = new Vector3(Random.Range(_trapArea.bounds.min.x, _trapArea.bounds.max.x), Random.Range(_trapArea.bounds.min.y, _trapArea.bounds.max.y), 0);
        _trapPositions[4] = new Vector3(Random.Range(_trapArea.bounds.min.x, _trapArea.bounds.max.x), Random.Range(_trapArea.bounds.min.y, _trapArea.bounds.max.y), 0);

        StartCoroutine(Root());
    }

    private IEnumerator Root()
    {
        // Hide the boss
        _animator.SetTrigger(HIDE);
        yield return WaitForPausedSeconds(2f);

        // Go to the i position and start the bullet system
        for (int i = 0; i < 4; i++)
        {
            MoveToDestination(_trapPositions[i], 0.25f);
            yield return WaitForPausedSeconds(0.5f);
            _animator.SetTrigger(TRAP);
            yield return WaitForPausedSeconds(0.5f);
            _bulletSystem.StartSpawner();
            yield return WaitForPausedSeconds(0.25f);
            _bulletSystem.StopSpawner();
            yield return WaitForPausedSeconds(0.25f);
        }

        // Go to the last position and start the bullet system
        MoveToDestination(_trapPositions[4], 0.25f);
        yield return WaitForPausedSeconds(0.5f);
        _animator.SetTrigger(ARISE);
        yield return WaitForPausedSeconds(1.5f);
        _bulletSystem.FireRate = 0.1f;
        _bulletSystem.SpinSpeed = 25;
        _bulletSystem.StartSpawner();
        yield return WaitForPausedSeconds(0.5f);
        _bulletSystem.StopSpawner();

        // Return to the start position
        _animator.SetTrigger(HIDE);
        yield return WaitForPausedSeconds(2f);
        MoveToDestination(new Vector3(0, 3, 0), 0.25f);
        yield return WaitForPausedSeconds(0.5f);
        _bulletSystem.InstantKill();
        _animator.SetTrigger(ARISE);
        yield return WaitForPausedSeconds(1.5f);

        // Start the move bullet system
        SetBulletMove();
        _bulletSystem.StartSpawner();

        // Finish the attack
        _isRooting = false;
    }
    #endregion

    private void HellAttack()
    {
        _isHell = true;

        // Start the hell attack
        StartCoroutine(Hell());
    }

    private IEnumerator Hell()
    {
        // Hide the boss and move it to the center
        _animator.SetTrigger(HIDE);
        yield return WaitForPausedSeconds(2f);
        MoveToDestination(Vector3.zero, 0.25f);
        yield return WaitForPausedSeconds(0.5f);
        _animator.SetTrigger(ARISE);
        yield return WaitForPausedSeconds(1.5f);

        // Start the hell attack
        _animator.SetBool(HELL, true);
        _bulletSystem.StartSpawner();
        yield return WaitForPausedSeconds(14f);
        _animator.SetBool(HELL, false);
        _bulletSystem.StopSpawner();

        // Return to the center
        _animator.SetTrigger(HIDE);
        yield return WaitForPausedSeconds(2f);
        MoveToDestination(new Vector3(0, 3, 0), 0.25f);
        yield return WaitForPausedSeconds(0.5f);
        _animator.SetTrigger(ARISE);
        yield return WaitForPausedSeconds(1.5f);

        // Start the move bullet system
        SetBulletMove();
        _bulletSystem.StartSpawner();

        // Stop the hell attack
        _isHell = false;
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
        Debug.Log("Floera is dead");

        // Make the boss solid
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);

        // Play the death animation
        _animator.SetTrigger(DEATH);
        yield return new WaitForSeconds(2.2f);

        // Wait for the boss to finish the death animation
        yield return new WaitForSeconds(2.25f);

        // Stop the animator
        _animator.enabled = false;

        // Save the score
        LevelScoreManager.Instance.SaveScore();

        // Load the main menu and resume the game
        yield return new WaitForSeconds(1.2f);
        SceneTransitionManager.Instance.LoadLevel(SCORE_SCREEN);
    }

    #region Bullet Patterns
    private void SetBulletMove()
    {
        _bulletSystem.CurrentPattern = BulletSystem.PatternType.StarShaped;
        _bulletSystem.SpawnerName = "FloeraMove";
        _bulletSystem.BulletSpeed = 6;
        _bulletSystem.NumberOfSides = 6;
        _bulletSystem.DotsPerSide = 4;
        _bulletSystem.FireRate = 0.65f;
        _bulletSystem.SpinSpeed = 150;
    }

    private void SetBulletTrap()
    {
        _bulletSystem.CurrentPattern = BulletSystem.PatternType.MultiSpiral;
        _bulletSystem.SpawnerName = "FloeraTrap";
        _bulletSystem.BulletSpeed = 8;
        _bulletSystem.NumberOfColumns = 25;
        _bulletSystem.FireRate = 10f;
        _bulletSystem.SpinSpeed = 0;
        _bulletSystem.UseReversedSpiral = false;
    }

    private void SetBulletHell()
    {
        _bulletSystem.CurrentPattern = BulletSystem.PatternType.MultiSpiral;
        _bulletSystem.SpawnerName = "FloeraHell";
        _bulletSystem.BulletSpeed = 4;
        _bulletSystem.NumberOfColumns = 5;
        _bulletSystem.FireRate = 0.15f;
        _bulletSystem.SpinSpeed = 50;
        _bulletSystem.UseReversedSpiral = true;
    }
    #endregion

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