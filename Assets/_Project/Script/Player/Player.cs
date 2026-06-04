using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private IVoiceInput _voiceInput;
    public float baseDamage = 100f;

    [Header("체력 및 경험치 (원형 UI)")]
    public float maxHealth = 100f;
    private float _currentHealth;
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float maxExp = 100f;

    // 기존 Slider 대신 Image 컴포넌트를 사용합니다.
    public Image hpRing;
    public Image expRing;

    [Header("전투 설정")]
    public GameObject projectilePrefab;
    public Transform firePoint; // 발사 위치 (비워두면 플레이어 몸에서 발사)

    [Header("결과 통계 (추적용)")]
    public int killCount = 0;           // 처치한 적 수
    public float totalAccuracyScore = 0f; // 누적 발음 점수
    public int validAttackCount = 0;    // 성공한 공격 횟수 (평균을 내기 위함)

    [Header("승리 조건 설정")]
    // 데모 시연용: N개의 카테고리를 마스터하면 승리! (0으로 두면 무조건 '모든 카테고리' 기준으로 작동합니다)
    public int targetCategoryCount = 3;

    void Start()
    {
        _voiceInput = FindObjectOfType<MockKeyboardInput>();
        if (_voiceInput != null) _voiceInput.OnWordSpoken += AttackTarget;

        _currentHealth = maxHealth;

        // 게임 시작 시 원형 UI 초기화
        UpdateUI();

        if (PronunciationClient.Instance != null)
            PronunciationClient.Instance.OnScoreReceived += ExecuteVoiceAttack;
    }

    private void ExecuteVoiceAttack(ScorePayload payload)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        string recognizedWord = payload.recognized_word;
        if (string.IsNullOrEmpty(recognizedWord)) return;

        Enemy[] targets = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in targets)
        {
            if (enemy.displayText == recognizedWord)
            {
                float damage = baseDamage * (payload.overall_score / 100f);

                // 즉시 데미지를 주지 않고 투사체를 발사합니다.
                ShootProjectile(enemy, damage);
                totalAccuracyScore += payload.overall_score;
                validAttackCount++;

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
        killCount++;

        // 적 처치 시 체력 증가 (원하시는 수치로 조절하세요!)
        if (_currentHealth < maxHealth)
        {
            _currentHealth += 2f;
            if (_currentHealth > maxHealth)
            {
                _currentHealth = maxHealth;
            }
        }

        if (currentExp >= maxExp) LevelUp();

        UpdateUI(); // 최대 체력이 늘었으니 원형 UI도 갱신!
    }

    private void LevelUp()
    {
        currentLevel++;
        currentExp -= maxExp;
        maxExp += 10f;

        // 레벨업 시 체력을 100% (최대치로) 회복!
        _currentHealth = maxHealth;

        UpdateUI();

        if (UIManager.Instance != null) UIManager.Instance.HideFeedbackImmediate();

        int totalCategories = 0;
        if (CategorySelectionUI.Instance != null)
        {
            totalCategories = CategorySelectionUI.Instance.CurrentDisplayedCategories.Count;
        }

        // 2. 실제 클리어 목표치 계산
        // N이 0보다 크면 N과 전체 개수 중 더 '작은 값'을 목표로 하고, N이 0이면 전체 개수를 목표로 합니다.
        int actualTarget = (targetCategoryCount > 0) ? Mathf.Min(targetCategoryCount, totalCategories) : totalCategories;

        // 3. 목표치 도달 검사
        if (LevelUpUI.UnlockedCategories.Count >= actualTarget)
        {
            Debug.Log($"[Player] 목표 카테고리({actualTarget}개) 마스터 완료! 퍼펙트 클리어!");

            ResultUI.IsClearData = true;
            GameManager.Instance.ChangeState(GameState.GameOver);
            return;
        }

        // 고를 단어장이 남아있을 때만 비로소 시간을 멈추고 레벨업 창을 띄웁니다.
        GameManager.Instance.ChangeState(GameState.LevelUp);
    }

    public void TakeDamage(float damageAmount)
    {
        _currentHealth -= damageAmount;
        if (_currentHealth < 0) _currentHealth = 0; // 체력이 0 미만으로 내려가지 않게 방어

        // 피격 후 UI 갱신
        UpdateUI();

        if (_currentHealth <= 0) Die();
    }

    // [핵심 로직] HP와 EXP 원형 UI를 갱신하는 헬퍼 함수
    private void UpdateUI()
    {
        // 전체 원(1.0)의 절반(0.5)만 사용하므로, 비율에 무조건 0.5f를 곱해줍니다.
        if (hpRing != null)
        {
            hpRing.fillAmount = (_currentHealth / maxHealth) * 0.5f;
        }

        if (expRing != null)
        {
            expRing.fillAmount = (currentExp / maxExp) * 0.5f;
        }
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
        ResultUI.IsClearData = false;
        GameManager.Instance.ChangeState(GameState.GameOver);
    }
}