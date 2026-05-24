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
        string recognizedWord = payload.recognized_word; // 서버는 "닭볶이"라고 보내줍니다.
        if (string.IsNullOrEmpty(recognizedWord)) return;

        Enemy[] targets = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in targets)
        {
            // 수정: matchText(닥뽀끼) 대신 displayText(닭볶이)를 비교해야 합니다!
            if (enemy.displayText == recognizedWord)
            {
                float damage = baseDamage * (payload.overall_score / 100f);

                // 공격 수행 및 생존 여부 확인
                bool isAlive = enemy.TakeDamage(damage);

                if (UIManager.Instance != null && payload.detailed_jamos != null)
                {
                    UIManager.Instance.ShowSyllableFeedback("<color=#00FF00>Hit!</color>", new List<JamoScoreInfo>(payload.detailed_jamos));
                }

                // 적이 살아있을 때만 단어 교체
                if (isAlive)
                {
                    EnemySpawner.Instance.ChangeEnemyWord(enemy, enemy.displayText);
                }
                return; // 타겟을 찾았으니 루프 종료
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
                enemy.TakeDamage(finalDamage);
                return;
            }
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