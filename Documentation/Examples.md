# Ludo EventBus - Examples

This document provides practical examples of using the Ludo EventBus system in common game development scenarios.

## Table of Contents

- [Basic Example](#basic-example)
- [Player Health System](#player-health-system)
- [Achievement System](#achievement-system)
- [Level Management](#level-management)
- [UI Updates](#ui-updates)
- [Input System Integration](#input-system-integration)

## Basic Example

A simple example showing the core concepts:

```csharp
// 1. Define an event
public class PlayerJumpedEvent : GameEvent
{
    public float JumpHeight { get; }
    
    public PlayerJumpedEvent(float jumpHeight)
    {
        JumpHeight = jumpHeight;
    }
}

// 2. Publish the event
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float jumpHeight = 2f;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Perform jump
            // ...
            
            // Publish event
            GameEventHub.Publish(new PlayerJumpedEvent(jumpHeight));
        }
    }
}

// 3. Subscribe to the event
public class JumpEffectsManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private AudioClip jumpSound;
    
    private AudioSource _audioSource;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(PlayerJumpedEvent))]
    private void OnPlayerJumped(PlayerJumpedEvent evt)
    {
        // Play effects
        jumpParticles.Play();
        _audioSource.PlayOneShot(jumpSound);
    }
}
```

## Player Health System

A more complex example showing event cancellation and priorities:

```csharp
// Events
public class PlayerDamagedEvent : GameEvent
{
    public int DamageAmount { get; }
    public string DamageSource { get; }
    
    public PlayerDamagedEvent(int damageAmount, string damageSource)
    {
        DamageAmount = damageAmount;
        DamageSource = damageSource;
    }
}

public class PlayerDiedEvent : GameEvent
{
    public string KilledBy { get; }
    
    public PlayerDiedEvent(string killedBy)
    {
        KilledBy = killedBy;
    }
}

// Health Manager
public class PlayerHealthManager : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invulnerabilityTime = 1f;
    
    private int _currentHealth;
    private bool _isInvulnerable;
    
    private void Awake()
    {
        _currentHealth = maxHealth;
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    // Essential priority - runs first and can cancel the event
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Essential)]
    private void CheckInvulnerability(PlayerDamagedEvent evt)
    {
        if (_isInvulnerable)
        {
            // Cancel the event - no damage will be applied
            evt.StopPropagation();
        }
    }
    
    // High priority - core game logic
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.High)]
    private void ApplyDamage(PlayerDamagedEvent evt)
    {
        // Apply damage
        _currentHealth -= evt.DamageAmount;
        
        // Start invulnerability
        StartCoroutine(InvulnerabilityCoroutine());
        
        // Check for death
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            GameEventHub.Publish(new PlayerDiedEvent(evt.DamageSource));
        }
    }
    
    private IEnumerator InvulnerabilityCoroutine()
    {
        _isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        _isInvulnerable = false;
    }
}

// UI Manager
public class HealthUIManager : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image damageFlashImage;
    
    private void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    // Medium priority - UI updates
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Medium)]
    private void UpdateHealthUI(PlayerDamagedEvent evt)
    {
        // Update health bar
        healthSlider.value = GetComponent<PlayerHealthManager>().CurrentHealth / (float)maxHealth;
        
        // Flash damage effect
        StartCoroutine(FlashDamage());
    }
    
    private IEnumerator FlashDamage()
    {
        damageFlashImage.enabled = true;
        yield return new WaitForSeconds(0.1f);
        damageFlashImage.enabled = false;
    }
}

// Analytics
public class GameAnalytics : MonoBehaviour
{
    private void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    // Cleanup priority - always runs, even if event was canceled
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Cleanup)]
    private void LogDamageEvent(PlayerDamagedEvent evt)
    {
        Debug.Log($"Player took {evt.DamageAmount} damage from {evt.DamageSource}");
        // Send to analytics service
    }
    
    [OnGameEvent(typeof(PlayerDiedEvent), SubscriberPriority.Cleanup)]
    private void LogDeathEvent(PlayerDiedEvent evt)
    {
        Debug.Log($"Player died. Killed by: {evt.KilledBy}");
        // Send to analytics service
    }
}
```

## Achievement System

Example of using the EventBus for an achievement system:

```csharp
// Achievement event
public class AchievementUnlockedEvent : GameEvent
{
    public string AchievementId { get; }
    public string AchievementName { get; }
    
    public AchievementUnlockedEvent(string id, string name)
    {
        AchievementId = id;
        AchievementName = name;
    }
}

// Achievement manager
public class AchievementManager : MonoBehaviour
{
    [Serializable]
    public class Achievement
    {
        public string Id;
        public string Name;
        public string Description;
        public bool Unlocked;
    }
    
    [SerializeField] private List<Achievement> achievements;
    
    private void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    // Listen for various game events to unlock achievements
    
    [OnGameEvent(typeof(PlayerDiedEvent))]
    private void CheckDeathAchievements(PlayerDiedEvent evt)
    {
        if (evt.KilledBy == "Fall")
        {
            UnlockAchievement("ACH_FALLING_DEATH");
        }
    }
    
    [OnGameEvent(typeof(EnemyKilledEvent))]
    private void CheckKillAchievements(EnemyKilledEvent evt)
    {
        // Track kill count and unlock achievements at certain milestones
        // ...
    }
    
    private void UnlockAchievement(string achievementId)
    {
        var achievement = achievements.Find(a => a.Id == achievementId);
        if (achievement != null && !achievement.Unlocked)
        {
            achievement.Unlocked = true;
            
            // Save achievement progress
            // ...
            
            // Publish achievement unlocked event
            GameEventHub.Publish(new AchievementUnlockedEvent(achievement.Id, achievement.Name));
        }
    }
}

// Achievement UI
public class AchievementUI : MonoBehaviour
{
    [SerializeField] private GameObject achievementPopup;
    [SerializeField] private Text achievementNameText;
    
    private void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(AchievementUnlockedEvent))]
    private void ShowAchievementPopup(AchievementUnlockedEvent evt)
    {
        achievementNameText.text = evt.AchievementName;
        achievementPopup.SetActive(true);
        
        // Hide after delay
        StartCoroutine(HidePopupAfterDelay(3f));
    }
    
    private IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        achievementPopup.SetActive(false);
    }
}
```

## Level Management

Example of using events for level transitions:

```csharp
// Level events
public class LevelStartEvent : GameEvent
{
    public int LevelNumber { get; }
    
    public LevelStartEvent(int levelNumber)
    {
        LevelNumber = levelNumber;
    }
}

public class LevelCompleteEvent : GameEvent
{
    public int LevelNumber { get; }
    public float CompletionTime { get; }
    
    public LevelCompleteEvent(int levelNumber, float completionTime)
    {
        LevelNumber = levelNumber;
        CompletionTime = completionTime;
    }
}

// Level manager
public class LevelManager : MonoBehaviour
{
    [SerializeField] private int currentLevel = 1;
    private float _levelStartTime;
    
    private void Start()
    {
        StartLevel(currentLevel);
    }
    
    public void StartLevel(int levelNumber)
    {
        currentLevel = levelNumber;
        _levelStartTime = Time.time;
        
        // Load level
        // ...
        
        // Publish level start event
        GameEventHub.Publish(new LevelStartEvent(currentLevel));
    }
    
    public void CompleteLevel()
    {
        float completionTime = Time.time - _levelStartTime;
        
        // Publish level complete event
        GameEventHub.Publish(new LevelCompleteEvent(currentLevel, completionTime));
        
        // Start next level after delay
        StartCoroutine(StartNextLevelAfterDelay(2f));
    }
    
    private IEnumerator StartNextLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartLevel(currentLevel + 1);
    }
}

// Level UI
public class LevelUI : MonoBehaviour
{
    [SerializeField] private Text levelText;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Text completionTimeText;
    
    private void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(LevelStartEvent))]
    private void OnLevelStart(LevelStartEvent evt)
    {
        levelText.text = $"Level {evt.LevelNumber}";
        levelCompletePanel.SetActive(false);
    }
    
    [OnGameEvent(typeof(LevelCompleteEvent))]
    private void OnLevelComplete(LevelCompleteEvent evt)
    {
        completionTimeText.text = $"Time: {evt.CompletionTime:F2}s";
        levelCompletePanel.SetActive(true);
    }
}
```

## UI Updates

Example of using events to update UI elements:

```csharp
// Score event
public class ScoreChangedEvent : GameEvent
{
    public int NewScore { get; }
    public int ScoreDelta { get; }
    
    public ScoreChangedEvent(int newScore, int scoreDelta)
    {
        NewScore = newScore;
        ScoreDelta = scoreDelta;
    }
}

// Score manager
public class ScoreManager : MonoBehaviour
{
    private int _currentScore = 0;
    
    public void AddScore(int points)
    {
        int oldScore = _currentScore;
        _currentScore += points;
        
        // Publish score changed event
        GameEventHub.Publish(new ScoreChangedEvent(_currentScore, _currentScore - oldScore));
    }
}

// Score UI
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject scorePopup;
    [SerializeField] private Text scorePopupText;
    
    private void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(ScoreChangedEvent))]
    private void UpdateScoreUI(ScoreChangedEvent evt)
    {
        // Update main score display
        scoreText.text = $"Score: {evt.NewScore}";
        
        // Show popup for score changes
        if (evt.ScoreDelta > 0)
        {
            ShowScorePopup(evt.ScoreDelta);
        }
    }
    
    private void ShowScorePopup(int points)
    {
        scorePopupText.text = $"+{points}";
        scorePopup.SetActive(true);
        
        // Hide after delay
        StartCoroutine(HidePopupAfterDelay(1f));
    }
    
    private IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        scorePopup.SetActive(false);
    }
}
```

## Input System Integration

Example of using events with an input system:

```csharp
// Input events
public class PlayerInputEvent : GameEvent
{
    // Base class for input events
}

public class PlayerMoveInputEvent : PlayerInputEvent
{
    public Vector2 MoveDirection { get; }
    
    public PlayerMoveInputEvent(Vector2 moveDirection)
    {
        MoveDirection = moveDirection;
    }
}

public class PlayerJumpInputEvent : PlayerInputEvent
{
    // Jump has no parameters
}

public class PlayerAttackInputEvent : PlayerInputEvent
{
    // Attack has no parameters
}

// Input manager
public class InputManager : MonoBehaviour
{
    private void Update()
    {
        // Movement
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (moveInput.sqrMagnitude > 0.01f)
        {
            GameEventHub.Publish(new PlayerMoveInputEvent(moveInput));
        }
        
        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameEventHub.Publish(new PlayerJumpInputEvent());
        }
        
        // Attack
        if (Input.GetMouseButtonDown(0))
        {
            GameEventHub.Publish(new PlayerAttackInputEvent());
        }
    }
}

// Player controller
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    
    private Rigidbody _rigidbody;
    private bool _isGrounded;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(PlayerMoveInputEvent))]
    private void OnMoveInput(PlayerMoveInputEvent evt)
    {
        // Apply movement
        Vector3 moveDirection = new Vector3(evt.MoveDirection.x, 0, evt.MoveDirection.y);
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
    
    [OnGameEvent(typeof(PlayerJumpInputEvent))]
    private void OnJumpInput(PlayerJumpInputEvent evt)
    {
        if (_isGrounded)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    [OnGameEvent(typeof(PlayerAttackInputEvent))]
    private void OnAttackInput(PlayerAttackInputEvent evt)
    {
        // Perform attack
        // ...
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }
}
```
