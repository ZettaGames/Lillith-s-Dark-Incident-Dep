using System.Collections;
using UnityEngine;

public class DarkyBehaviour : BossGeneric
{
    // CONSTANTS
    // States
    private const string THUNDER = "thunder";
    private const string END = "end";
    private const string FAKE = "fake";
    private const string DEATH = "death";

    // Scenes
    private const string SCORE_SCREEN = "ScoreScreen";

    // Darky Phrases
    private string[] _phrases = new string[]
    {
        "Behold the darkened star!",
        "Cloudy… with a guarantee of darkness",
        "Dark... Darker... Yet darker...",
        "You dare fight the wind?",
        "A dark incident approach...",
        "Incident? No. Ascension!"
    };

    [Header("Darky Settings")]
    [SerializeField] private float[] _attackProbabilies = new float[3];
    [SerializeField] private AudioClip _finalMusic;

    [Header("Thunders")]
    [SerializeField] private Collider2D _spawnArea;
    [SerializeField] private Collider2D _centerArea;
    [SerializeField] private GameObject _lightThunder;
    [SerializeField] private GameObject _darkThunder;
    [SerializeField] private GameObject _warningSign;
    [SerializeField] private AudioClip _thunderClip;
    private GameObject _thunderToUse;

    [Header("Cloud Walls")]
    [SerializeField] private GameObject _lightLeft;
    [SerializeField] private GameObject _lightRight;
    [SerializeField] private GameObject _darkLeft;
    [SerializeField] private GameObject _darkRight;
    private GameObject _leftToUse;
    private GameObject _rightToUse;

    [Header("Components")]
    [SerializeField] private BulletSystem _rainSystem;
    [SerializeField] private BulletSystem _darkySystem;
    [SerializeField] private Animator _animator;

    [Header("Backgrounds")]
    [SerializeField] private GameObject _lightBackground;
    [SerializeField] private GameObject _darkBackground;

    [SerializeField] private Shader _backgroundBlendShader;
    private Material[] _phaseBackgroundMaterials;
    private Sprite[] _phaseBackgroundSprites;
    private const float PhaseFadeDuration = 1.5f;
    // Center the background blend on the end of DarkyFakeDeath.anim (5.25 seconds).
    private const float TransformationDuration = 5.25f;
    private static readonly int PhaseBlend = Shader.PropertyToID("_PhaseBlend");

    private Vector3 _left = new Vector3(-3f, 3f, 0f);
    private Vector3 _right = new Vector3(3f, 3f, 0f);
    private Vector3 _center = new Vector3(0f, 0f, 0f);

    private bool _canWander = true;
    private bool _isThunder;
    private bool _isCloud;
    private bool _isHell;
    private bool _isPhaseTwo = false;
    private bool _isPhaseThree = false;

    private DarkyAttackType _currentAttackType;
    private int _previousAttackType = (int)DarkyAttackType.Hell;
    private enum DarkyAttackType
    {
        ThunderAttack,
        CloudWalls,
        Hell
    }

    private void Start()
    {
        _audioSource = _musicPlayer.GetComponent<AudioSource>();

        _currentHealth = _maxHealth;
        PreparePhaseBackgrounds();

        StartCoroutine(ChangePhrase());
        StartCoroutine(Wander());

        _timer = _timeBetweenAttacks;
    }

    private void Update()
    {
        if (LocalTime.IsPaused || _isPhaseThree) return;
        // Change the thunder and cloud walls to use
        _thunderToUse = _isPhaseTwo ? _darkThunder : _lightThunder;
        _leftToUse = _isPhaseTwo ? _darkLeft : _lightLeft;
        _rightToUse = _isPhaseTwo ? _darkRight : _lightRight;

        // Boss control
        _healthBar.fillAmount = (float)_currentHealth / _maxHealth;
        _timer += Time.deltaTime * LocalTime.TimeScale;

        // Change the attack if the boss is not thundering, clouding or in hell
        if (!_isThunder && !_isCloud && !_isHell)
        {
            // Stop all current bullet patterns
            _rainSystem.StopSpawner();
            _darkySystem.StopSpawner();

            // Set the star pattern
            SetStarPattern();

            // Select a new attack
            _currentAttackType = (DarkyAttackType)Choose(_attackProbabilies, _previousAttackType);
            _previousAttackType = (int)_currentAttackType;
            switch (_currentAttackType)
            {
                case DarkyAttackType.ThunderAttack:
                    _isThunder = true;
                    _rainSystem.StartSpawner();
                    _darkySystem.StartSpawner();

                    var thunders = _isPhaseTwo ? 8 : 5;

                    StartCoroutine(Thunder(thunders, _spawnArea));
                    break;
                case DarkyAttackType.CloudWalls:
                    _isCloud = true;
                    _rainSystem.StartSpawner();
                    _darkySystem.StartSpawner();
                    StartCoroutine(CloudWall());
                    break;
                case DarkyAttackType.Hell:
                    _isHell = true;
                    _canWander = false;
                    StopCoroutine(Wander());
                    StartCoroutine(Hell());
                    break;
            }
        }
    }

    private IEnumerator Wander()
    {
        while (_canWander)
        {
            Debug.Log("Wandering");

            // Move to the left
            MoveToDestination(_left, 2f);
            yield return WaitForPausedSeconds(2.25f);
            // Move to the right
            MoveToDestination(_right, 2f);
            yield return WaitForPausedSeconds(2.25f);
        }
    }

    private IEnumerator Thunder(int times, Collider2D area)
    {
        // Select a random position in the spawn area
        Vector3 position = new Vector3(Random.Range(area.bounds.min.x, area.bounds.max.x), 4f, 0f);

        // Show the warning sign
        var sign = Instantiate(_warningSign, position, Quaternion.identity);

        // Wait for the warning sign to show
        yield return WaitForPausedSeconds(1.0f);
        Destroy(sign);

        // Show the thunder and play the sound
        var thunder = Instantiate(_thunderToUse, position + new Vector3(0f, -4f, 0f), Quaternion.identity);
        SoundFXManager.Instance.PlaySoundFXClip(_thunderClip, transform, 0.75f);

        // Shake the screen
        ScreenShake.Instance.Shake(0.15f, 0.25f);

        // Wait for the thunder to show
        yield return WaitForPausedSeconds(0.5f);

        // Repeat the process for the given times
        if (times > 0)
        {
            Destroy(thunder);
            StartCoroutine(Thunder(times - 1, area));
        }
        else
        {
            Destroy(thunder);
            _isThunder = false;
        }
    }

    private IEnumerator CloudWall()
    {
        // Select between the left and right wall
        var wall = Random.Range(0, 2) == 0 ? _leftToUse : _rightToUse;

        Debug.Log("Wall selected: " + wall.name);

        // Fade in the wall
        var opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale * 0.5f;
            wall.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            yield return null;
        }

        // Activate the collider
        wall.GetComponent<Collider2D>().enabled = true;

        // Select between the two child walls
        var child = Random.Range(0, 2) == 0 ? wall.transform.GetChild(0) : wall.transform.GetChild(1);

        // Fade in the child wall
        opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale * 0.5f;
            child.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            yield return null;
        }

        // Activate the collider
        child.GetComponent<Collider2D>().enabled = true;

        // Select between the final two child walls
        var finalChild = child.transform.GetChild(0);

        // Fade in the final child wall
        opacity = 0f;
        while (opacity < 1)
        {
            opacity += Time.deltaTime * LocalTime.TimeScale * 0.5f;
            finalChild.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            yield return null;
        }

        // Activate the collider
        finalChild.GetComponent<Collider2D>().enabled = true;

        // Add thunders if it's phase two
        if (_isPhaseTwo)
        {
            // Get wall child with a collider
            var collider = wall.transform.GetChild(2).GetComponent<Collider2D>();

            // Add thunders
            StartCoroutine(Thunder(3, collider));
        }

        // Wait for the wall to show
        yield return WaitForPausedSeconds(2.5f);


        // Fade out all the walls, deactivate the colliders when the opacity is 0.5 and fade out the wall
        opacity = 1f;
        while (opacity > 0.75)
        {
            opacity -= Time.deltaTime * LocalTime.TimeScale;
            finalChild.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            child.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            wall.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            yield return null;
        }

        // Deactivate the colliders
        finalChild.GetComponent<Collider2D>().enabled = false;
        child.GetComponent<Collider2D>().enabled = false;
        wall.GetComponent<Collider2D>().enabled = false;

        // Fade out the wall
        while (opacity > 0)
        {
            opacity -= Time.deltaTime * LocalTime.TimeScale;
            finalChild.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            child.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            wall.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
            yield return null;
        }

        // Wait for the wall to fade out
        yield return WaitForPausedSeconds(0.75f);

        // Finish the cloud wall attack
        _isCloud = false;
    }

    private IEnumerator Hell()
    {
        // Stop the wander and wait for the boss to stop
        _canWander = false;
        StopCoroutine(Wander());
        yield return WaitForPausedSeconds(4f);

        // Move the boss to the center
        MoveToDestination(_center, 1.25f);

        // Wait for the boss to move
        yield return WaitForPausedSeconds(1.25f);

        // Start the animation
        _animator.SetTrigger(THUNDER);
        yield return WaitForPausedSeconds(1.25f);

        // Start the bullet pattern and shoot some thunders
        SetHellPattern();
        _darkySystem.StartSpawner();

        // Shoot the thunders
        StartCoroutine(TinyThunder());
        yield return WaitForPausedSeconds(12f);

        // Stop the bullet pattern
        _darkySystem.StopSpawner();
        _animator.SetTrigger(END);
        yield return WaitForPausedSeconds(0.75f);

        // Move the boss to the start position
        MoveToDestination(new Vector3(0f, 3f, 0f), 1.25f);
        yield return WaitForPausedSeconds(1.25f);
        _canWander = true;
        StartCoroutine(Wander());
        _isHell = false;
    }

    private IEnumerator TinyThunder()
    {
        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(Thunder(1, _centerArea));

            var time = _isPhaseTwo ? 3.5f : 5f;
            yield return new WaitForSeconds(time);
        }
    }

    private IEnumerator FadeOutWalls()
    {
        // Select the walls to fade out
        var walls = new GameObject[] { _lightLeft, _lightRight };
        // Select all child walls
        var childWalls = new GameObject[] { walls[0].transform.GetChild(0).gameObject, walls[0].transform.GetChild(1).gameObject, walls[1].transform.GetChild(0).gameObject, walls[1].transform.GetChild(1).gameObject };
        // Select all final child walls
        var finalChildWalls = new GameObject[] { childWalls[0].transform.GetChild(0).gameObject, childWalls[1].transform.GetChild(0).gameObject, childWalls[2].transform.GetChild(0).gameObject, childWalls[3].transform.GetChild(0).gameObject };

        // Deactivate the colliders
        foreach (var wall in walls)
        {
            wall.GetComponent<Collider2D>().enabled = false;
        }

        foreach (var child in childWalls)
        {
            child.GetComponent<Collider2D>().enabled = false;
        }

        foreach (var finalChild in finalChildWalls)
        {
            finalChild.GetComponent<Collider2D>().enabled = false;
        }

        // Fade out all the walls
        foreach (var wall in walls)
        {
            if (wall.GetComponent<SpriteRenderer>().color.a == 0)
            {
                break;
            }

            var opacity = 1f;
            while (opacity > 0)
            {
                opacity -= Time.deltaTime * LocalTime.TimeScale;
                wall.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
                yield return null;
            }
        }

        foreach (var child in childWalls)
        {
            if (child.GetComponent<SpriteRenderer>().color.a == 0)
            {
                break;
            }

            var opacity = 1f;
            while (opacity > 0)
            {
                opacity -= Time.deltaTime * LocalTime.TimeScale;
                child.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
                yield return null;
            }
        }

        foreach (var finalChild in finalChildWalls)
        {
            if (finalChild.GetComponent<SpriteRenderer>().color.a == 0)
            {
                break;
            }

            var opacity = 1f;
            while (opacity > 0)
            {
                opacity -= Time.deltaTime * LocalTime.TimeScale;
                finalChild.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, opacity);
                yield return null;
            }
        }

        // Deactivate the walls
        _lightLeft.SetActive(false);
        _lightRight.SetActive(false);
    }

    protected override void Death()
    {
        if (!_isPhaseTwo && !_isPhaseThree)
        {
            // Make the boss invulnerable
            GetComponent<Collider2D>().enabled = false;

            _isPhaseTwo = true;

            StopAllCoroutines();
            StartCoroutine(FakeDeath());
        }
        else if (_isPhaseTwo && !_isPhaseThree)
        {
            // Make the boss invulnerable
            GetComponent<Collider2D>().enabled = false;

            // Commit the final phase before any coroutine can yield or restart an attack.
            _isPhaseThree = true;
            _canWander = false;
            _isThunder = _isCloud = _isHell = true;
            StopAllCoroutines();
            ClearPhaseWalls();
            _stateText.text = "S U R V I V E";
            StartCoroutine(FinalHell());
        }
    }

    private void ClearPhaseWalls()
    {
        // CloudWall may have been cancelled before its collider cleanup, even at zero alpha.
        // Include both phases and inactive descendants; opacity never determines collision state.
        var walls = new[] { _lightLeft, _lightRight, _darkLeft, _darkRight };
        foreach (var wall in walls)
        {
            if (wall == null) continue;
            foreach (var collider in wall.GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;
            wall.SetActive(false);
        }
    }

    private IEnumerator FinalHell()
    {
        // Stop the wander and wait for the boss to stop
        _canWander = false;
        StopCoroutine(Wander());
        yield return WaitForPausedSeconds(1.5f);

        // Move the boss to the center
        MoveToDestination(_center, 1.25f);
        yield return WaitForPausedSeconds(1.25f);

        // Check if there is any warning sign in hierarchy and destroy it
        var warningSign = GameObject.Find("WarningSign(Clone)");
        if (warningSign != null)
        {
            Destroy(warningSign);
        }

        // Fade out the music
        while (_audioSource.volume > 0)
        {
            _audioSource.volume -= Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }

        // Change the music
        _audioSource.clip = _finalMusic;
        _audioSource.Play();
        _audioSource.loop = false;

        // Fade in the music
        while (_audioSource.volume < 1)
        {
            _audioSource.volume += Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }

        // Stop all behaviors
        _isThunder = true;
        _isCloud = true;
        _isHell = true;
        _darkySystem.StopSpawner();
        _rainSystem.StopSpawner();

        // Start the animation
        _animator.SetTrigger(THUNDER);
        yield return WaitForPausedSeconds(1.25f);

        // Start the final hell bullet pattern
        SetFinalHell();
        _darkySystem.StartSpawner();
        yield return WaitForPausedSeconds(15f);

        // Stop the bullet pattern
        _darkySystem.StopSpawner();
        _animator.SetTrigger(DEATH);
        yield return WaitForPausedSeconds(5f);

        // Save the score
        LevelScoreManager.Instance.SaveScore();

        // Load the main menu and resume the game
        yield return WaitForPausedSeconds(1.2f);
        SceneTransitionManager.Instance.LoadLevel(SCORE_SCREEN);
    }

    private IEnumerator FakeDeath()
    {
        _canWander = false;
        _isThunder = _isCloud = _isHell = true;

        // Smootly decrease the music volume
        while (_audioSource.volume > 0)
        {
            _audioSource.volume -= Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }

        // Check if there is any warning sign in hierarchy and destroy it
        var warningSign = GameObject.Find("WarningSign(Clone)");
        if (warningSign != null)
        {
            Destroy(warningSign);
        }

        // Stop all behaviors
        _canWander = false;
        StopCoroutine(Wander());
        _isThunder = true;
        _isCloud = true;
        _isHell = true;
        _darkySystem.StopSpawner();
        _rainSystem.StopSpawner();

        // Fade out the walls if boss die during the cloud wall attack
        StartCoroutine(FadeOutWalls());

        // Move the boss to the center
        yield return MoveForPhaseTransition(_center, 1.25f);

        // Start the animation
        _animator.SetTrigger(FAKE);
        yield return WaitForPausedSeconds(TransformationDuration - PhaseFadeDuration * 0.5f);
        yield return BlendPhaseVisuals();

        // Change the music 
        _audioSource.clip = _music;
        _audioSource.Play();

        // Smoothly increase the music volume
        while (_audioSource.volume < 1)
        {
            _audioSource.volume += Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }

        // Smoothly increase the health
        _maxHealth += 250;
        while (_currentHealth < _maxHealth)
        {
            _currentHealth += 500;
            yield return WaitForPausedSeconds(0.1f);
        }
        _currentHealth = _maxHealth;

        // Wait for the music to start
        yield return WaitForPausedSeconds(1.5f);

        // Move the boss to the start position
        yield return MoveForPhaseTransition(new Vector3(0f, 3f, 0f), 1.25f);

        // Make the boss vulnerable
        GetComponent<Collider2D>().enabled = true;

        // Reanude the behaviors
        _canWander = true;
        StartCoroutine(Wander());
        StartCoroutine(ChangePhrase());
        _isThunder = false;
        _isCloud = false;
        _isHell = false;
    }

    private void PreparePhaseBackgrounds()
    {
        if (_backgroundBlendShader == null) return;
        var light = _lightBackground.GetComponentsInChildren<SpriteRenderer>(true);
        var dark = _darkBackground.GetComponentsInChildren<SpriteRenderer>(true);
        _phaseBackgroundMaterials = new Material[light.Length];
        _phaseBackgroundSprites = new Sprite[light.Length];
        for (int i = 0; i < light.Length; i++)
        {
            foreach (var target in dark)
            {
                if (target.sortingOrder != light[i].sortingOrder || target.sprite == null) continue;
                // Full rectangles prevent the light texture's tight mesh clipping the dark clouds.
                var originalSprite = light[i].sprite;
                var pivot = new Vector2(originalSprite.pivot.x / originalSprite.rect.width,
                    originalSprite.pivot.y / originalSprite.rect.height);
                var fullRect = Sprite.Create(originalSprite.texture, originalSprite.rect, pivot,
                    originalSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                light[i].sprite = fullRect;
                _phaseBackgroundSprites[i] = fullRect;
                // Change only this renderer's instance, preserving the scroller's cached material.
                var material = light[i].material;
                var offset = material.mainTextureOffset;
                var scale = material.mainTextureScale;
                material.shader = _backgroundBlendShader;
                material.mainTextureOffset = offset;
                material.mainTextureScale = scale;
                material.SetTexture("_PhaseTex", target.sprite.texture);
                material.SetFloat(PhaseBlend, 0f);
                _phaseBackgroundMaterials[i] = material;
                break;
            }
        }
        // Both textures are sampled during phase one, before the actual transformation.
        // Reuse the same scrolling geometry for phase two; no material creation or activation spike.
    }

    private IEnumerator BlendPhaseVisuals()
    {
        float elapsed = 0f;
        while (elapsed < PhaseFadeDuration)
        {
            float t = Mathf.Clamp01(elapsed / PhaseFadeDuration);
            float blend = Mathf.SmoothStep(0f, 1f, t);
            if (_phaseBackgroundMaterials != null)
                foreach (var material in _phaseBackgroundMaterials)
                    if (material != null) material.SetFloat(PhaseBlend, blend);
            elapsed += Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }
        if (_phaseBackgroundMaterials != null)
        {
            foreach (var material in _phaseBackgroundMaterials)
                if (material != null) material.SetFloat(PhaseBlend, 1f);
        }
        else
        {
            _lightBackground.SetActive(false);
            _darkBackground.SetActive(true);
        }
    }

    private IEnumerator MoveForPhaseTransition(Vector3 destination, float duration)
    {
        Vector3 origin = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(origin, destination,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            elapsed += Time.deltaTime * LocalTime.TimeScale;
            yield return null;
        }
        transform.position = destination;
    }

    private void OnDestroy()
    {
        if (_phaseBackgroundMaterials == null) return;
        foreach (var material in _phaseBackgroundMaterials)
            if (material != null) Destroy(material);
        foreach (var sprite in _phaseBackgroundSprites)
            if (sprite != null) Destroy(sprite);
    }

    private void SetHellPattern()
    {
        _darkySystem.CurrentPattern = BulletSystem.PatternType.StarShaped;
        _darkySystem.name = "DarkyHell";
        _darkySystem.BulletSpeed = 3.5f;
        _darkySystem.BulletSize = 0.4f;
        _darkySystem.NumberOfSides = 5;
        _darkySystem.DotsPerSide = 3;
        _darkySystem.SpinSpeed = 10;
        _darkySystem.FireRate = 0.6f;
    }

    private void SetFinalHell()
    {
        _darkySystem.CurrentPattern = BulletSystem.PatternType.StarShaped;
        _darkySystem.name = "DarkyFinalHell";
        _darkySystem.BulletSpeed = 4f;
        _darkySystem.BulletSize = 0.4f;
        _darkySystem.NumberOfSides = 8;
        _darkySystem.DotsPerSide = 1;
        _darkySystem.SpinSpeed = 125;
        _darkySystem.FireRate = 0.3f;
    }

    private void SetStarPattern()
    {
        _darkySystem.CurrentPattern = BulletSystem.PatternType.StarShaped;
        _darkySystem.name = "DarkyStar";
        _darkySystem.BulletSpeed = 6f;
        _darkySystem.BulletSize = 0.4f;
        _darkySystem.NumberOfSides = 5;
        _darkySystem.DotsPerSide = 3;
        _darkySystem.SpinSpeed = 0;
        _darkySystem.FireRate = 1f;
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