using System.Collections;
using UnityEngine;

public class EnemyGeneric : MonoBehaviour
{
    // CONSTANTS
    private const string BulletTag = "Bullet";
    private const string SuperTag = "Super";
    private const string PoolTag = "EnemyPool";
    private const string NoCollision = "NoCollision";
    private const string Finish = "Finish";
    private const string UNTAGGED = "Untagged";
    // private const string Check = "Check";

    // Enemy Settings
    [Header("Enemy Settings")]
    [SerializeField] private int _health;
    [SerializeField] protected float speed;

    // Enemy Drops
    [Header("Enemy Drops")]
    [SerializeField] private GameObject _scoreDrop;
    [SerializeField] private GameObject _starDrop;
    [SerializeField] private float _scoreDropChance = 0.3f;
    [SerializeField] private float _starDropChance = 0.1f;
    private float _dropRadius = 0.5f;
    private bool _isDead;
    private Coroutine _damageFlash;
    private bool _flashPending;
    private Color _normalColor;

    // Components
    private SpriteRenderer _spriteRenderer;
    protected BulletSystem bulletSystem;

    private void Start()
    {
        // Initialization of components
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _normalColor = _spriteRenderer.color;
        bulletSystem = GetComponent<BulletSystem>();
        bulletSystem.StartSpawner();

        // Start the behaviour
        StartCoroutine(Behaviour());
    }

    protected virtual IEnumerator Behaviour()
    {
        // To be overridden by the child class
        yield return null;
    }

    private void TakeDamage()
    {
        if (_isDead) return;
        _health--;
        if (_health <= 0)
        {
            Die();
            return;
        }

        _flashPending = true;
        if (_damageFlash == null) _damageFlash = StartCoroutine(FlashDamage());
    }

    private IEnumerator FlashDamage()
    {
        do
        {
            _flashPending = false;
            _spriteRenderer.color = new Color(1f, 0f, 0f, _normalColor.a);
            yield return new WaitForSeconds(0.05f);
            _spriteRenderer.color = _normalColor;
            // Never restart this interval: sustained fire must still show normal frames.
            yield return new WaitForSeconds(0.05f);
        } while (_flashPending);
        _damageFlash = null;
    }

    private void Die()
    {
        // Commit death before any wait: other contacts in this frame must be ignored.
        _isDead = true;
        StopAllCoroutines();
        gameObject.tag = NoCollision;
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        var body = GetComponent<Rigidbody2D>();
        if (body != null) body.simulated = false;
        _spriteRenderer.enabled = false;
        bulletSystem.StopSpawner();

        // Roll each reward exactly once, at the position where the enemy died.
        if (Random.value < _scoreDropChance)
        {
            Instantiate(_scoreDrop, transform.position + Vector3.up * _dropRadius, Quaternion.identity);
            Instantiate(_scoreDrop, transform.position + Vector3.left * _dropRadius, Quaternion.identity);
            Instantiate(_scoreDrop, transform.position + Vector3.right * _dropRadius, Quaternion.identity);
        }
        if (Random.value < _starDropChance)
            Instantiate(_starDrop, transform.position + Vector3.down * _dropRadius, Quaternion.identity);

        StartCoroutine(RemoveAfterBullets());
    }

    private IEnumerator RemoveAfterBullets()
    {
        // Keep existing world-space bullets alive, then remove the invisible enemy.
        yield return new WaitForSeconds(bulletSystem.BulletLifeTime);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDead || LocalTime.TimeScale <= 0f) return;
        // Check if the enemy is hit by a bullet
        if (collision.CompareTag(BulletTag) && !this.CompareTag(UNTAGGED))
        {
            // Update the score
            LevelScoreManager.Instance.EnemyHitBonus();

            // Take damage
            TakeDamage();
            // The fatal hit changes our tag before the bullet's own callback may run.
            collision.gameObject.SetActive(false);
        }

        // Check if the enemy is hit by a super bullet
        if (collision.CompareTag(SuperTag) && !this.CompareTag(UNTAGGED))
        {
            // Update the score
            LevelScoreManager.Instance.SuperHitBonus();

            // Take damage
            _health = 1;
            TakeDamage();
        }

        // Check if the enemy reaches the end of the screen
        if (collision.CompareTag(Finish))
        {
            // Stop the spawner
            bulletSystem.StopSpawner();
        }

        // Check if the enemy is hit by the pool
        if (collision.CompareTag(PoolTag))
        {
            // Stop the spawner
            bulletSystem.InstantKill();

            // Destroy the enemy
            Destroy(gameObject);
        }
    }
}
