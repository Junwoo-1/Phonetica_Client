using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private IVoiceInput _voiceInput;
    public float baseDamage = 100f;

    [Header("체력 및 경험치")]
    public float maxHealth = 100f;
    private float _currentHealth;
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float maxExp = 100f;
    public Slider healthBarSlider;
    public Slider expBarSlider;

    [Header("전투 설정")]
    public GameObject projectilePrefab;
    public Transform firePoint; // 발사 위치 (비워두면 플레이어 몸에서 발사)

    void Start()
    {
        _voiceInput = FindObjectOfType<MockKeyboardInput>();
        if (_voiceInput != null) _voiceInput.OnWordSpoken += AttackTarget;

        _currentHealth = maxHealth;
        if (healthBarSlider != null) { healthBarSlider.maxValue = maxHealth; healthBarSlider.value = _currentHealth; }
        if (expBarSlider != null) { expBarSlider.maxValue = maxExp; expBarSlider.value = currentExp; }

        if (PronunciationClient.Instance != null)
            PronunciationClient.Instance.OnScoreReceived += ExecuteVoiceAttack;
    }

    private void ExecuteVoiceAttack(ScorePayload payload)
    {
        string recognizedWord = payload.recognized_word;
        if (string.IsNullOrEmpty(recognizedWord)) return;

        Enemy[] targets = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in targets)
        {
            if (enemy.displayText == recognizedWord)
            {
                float damage = baseDamage * (payload.overall_score / 100f);

                // 수정됨: 즉시 데미지를 주지 않고 투사체를 발사합니다.
                ShootProjectile(enemy, damage);

                if (UIManager.Instance != null && payload.detailed_jamos != null)
                {
                    UIManager.Instance.ShowSyllableFeedback("<color=#00FF00>Hit!</color>", new List<JamoScoreInfo>(payload.detailed_jamos), new List<JamoToken>(payload.heard_jamos));
                }

                return;
            }
        }
    }
    private void AttackTarget(VoiceData data)
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy.matchText == data.word)
            {
                float finalDamage = baseDamage * (data.accuracy + data.pitchScore);
                ShootProjectile(enemy, finalDamage); // 투사체 발사로 변경
                return;
            }
        }
    }

    private void ShootProjectile(Enemy target, float damage)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(target, damage); // 투사체에게 타겟과 데미지를 넘겨줍니다.
        }
    }

    public void AddExp(float expAmount)
    {
        currentExp += expAmount;
        if (expBarSlider != null) expBarSlider.value = currentExp;
        if (currentExp >= maxExp) LevelUp();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentExp -= maxExp;
        maxExp *= 1.5f;
        if (expBarSlider != null)
        {
            expBarSlider.maxValue = maxExp;
            expBarSlider.value = currentExp;
        }
        GameManager.Instance.ChangeState(GameState.LevelUp);
    }

    public void TakeDamage(float damageAmount)
    {
        _currentHealth -= damageAmount;
        if (healthBarSlider != null) healthBarSlider.value = _currentHealth;
        if (_currentHealth <= 0) Die();
    }

    private string GetColorCode(float score)
    {
        if (score >= 90) return "#00FF00"; // 초록
        if (score >= 60) return "#FFFF00"; // 노랑
        return "#FF0000";                  // 빨강
    }

    void OnDestroy()
    {
        if (_voiceInput != null) _voiceInput.OnWordSpoken -= AttackTarget;
        if (PronunciationClient.Instance != null) PronunciationClient.Instance.OnScoreReceived -= ExecuteVoiceAttack;
    }

    private void Die()
    {
        GameManager.Instance.ChangeState(GameState.GameOver);
    }
}