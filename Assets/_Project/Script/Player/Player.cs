using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // 인터페이스를 통해 입력기를 받습니다.
    private IVoiceInput _voiceInput;
    public float baseDamage = 10f;

    [Header("체력 시스템")]
    public float maxHealth = 100f;
    private float _currentHealth;

    [Header("경험치 시스템")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float maxExp = 100f; // 레벨업에 필요한 경험치량

    public Slider healthBarSlider;
    public Slider expBarSlider;
    void Start()
    {
        // 씬에 있는 가짜 입력기를 찾아서 연결합니다. (나중에 STT로 교체할 부분)
        _voiceInput = FindObjectOfType<MockKeyboardInput>();
        
        if (_voiceInput != null)
        {
            // 입력기의 이벤트에 내 공격 함수를 구독(연결)합니다.
            _voiceInput.OnWordSpoken += AttackTarget;
        }
        // 체력 초기화
        _currentHealth = maxHealth;
        
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth; // 슬라이더의 최댓값을 내 최대 체력으로 맞춤
            healthBarSlider.value = _currentHealth; // 현재 슬라이더 게이지를 꽉 채움
        }
        // 경험치바 초기화
        if (expBarSlider != null)
        {
            expBarSlider.maxValue = maxExp;
            expBarSlider.value = currentExp;
        }

        // 네트워크 매니저로부터 점수(Payload)가 도착하면 실행될 함수를 연결합니다.
        if (PronunciationClient.Instance != null)
        {
            PronunciationClient.Instance.OnScoreReceived += ExecuteVoiceAttack;
        }
    }

    // ⭐️ 서버에서 점수와 인식된 단어를 받았을 때 실행되는 핵심 함수
    private void ExecuteVoiceAttack(ScorePayload payload)
    {
        Debug.Log($"[공격 시도] 인식된 단어: {payload.recognized_word}, 점수: {payload.overall_score}");

        // 1. 씬에 있는 모든 적(Enemy)을 찾습니다.
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        bool targetFound = false;

        foreach (Enemy enemy in allEnemies)
        {
            // 2. 적이 가진 단어(myWord)와 서버가 인식한 단어(recognized_word)가 일치하는지 확인
            if (enemy.myWord == payload.recognized_word)
            {
                // 3. 데미지 계산: (기본 데미지 * 점수 백분율)
                // 보고서 가이드에 따라 overall_score(0~100)를 0.0~1.0 계수로 변환합니다[cite: 53].
                float damageMultiplier = payload.overall_score / 100f;
                float finalDamage = baseDamage * damageMultiplier;

                // 4. 적에게 데미지 입히기
                enemy.TakeDamage(finalDamage);
                
                targetFound = true;
                Debug.Log($"🎯 {enemy.myWord} 타격 성공! 데미지: {finalDamage}");
                
                // (선택 사항) 서바이버 장르 특성상 같은 단어가 여러 명일 수 있다면 
                // return 대신 계속 루프를 돌게 할 수 있습니다.
            }
        }

        if (!targetFound)
        {
            Debug.LogWarning($"일치하는 단어({payload.recognized_word})를 가진 적이 화면에 없습니다.");
        }
    }

    private void AttackTarget(VoiceData data)
    {
        // 1. 씬에 있는 모든 적을 찾습니다. (임시 로직)
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        // 2. 내가 말한 단어와 똑같은 단어를 가진 적을 찾습니다.
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy.myWord == data.word)
            {
                // 3. 기획했던 데미지 공식을 적용합니다.
                float finalDamage = baseDamage * (data.accuracy + data.pitchScore);
                
                enemy.TakeDamage(finalDamage);
                return; // 하나 때렸으면 함수 종료
            }
        }

        Debug.Log("일치하는 단어를 가진 적이 없습니다!");
    }

    public void AddExp(float expAmount)
    {
        currentExp += expAmount;
        Debug.Log($"경험치 획득: +{expAmount} (현재: {currentExp}/{maxExp})");

        // UI 경험치바 채우기
        if (expBarSlider != null) expBarSlider.value = currentExp;

        // 레벨업 조건 달성 시
        if (currentExp >= maxExp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        currentExp -= maxExp; // 초과한 경험치는 이월시킴 (예: 120/100 이면 다음 레벨은 20부터 시작)
        maxExp *= 1.5f;       // 다음 레벨업 요구치를 1.5배로 증가 (기획에 맞게 수정)

        // UI 갱신 (max값이 바뀌었으므로)
        if (expBarSlider != null)
        {
            expBarSlider.maxValue = maxExp;
            expBarSlider.value = currentExp;
        }

        Debug.Log($"레벨 업! 현재 레벨: {currentLevel}");
        
        // ⭐️ GameManager를 통해 시간을 멈추고 업그레이드 창(LevelUp State)을 띄웁니다!
        GameManager.Instance.ChangeState(GameState.LevelUp);
    }

    public void TakeDamage(float damageAmount)
    {
        _currentHealth -= damageAmount; // 체력 깎기
        
        // UI 슬라이더 업데이트
        if (healthBarSlider != null)
        {
            healthBarSlider.value = _currentHealth;
        }

        Debug.Log($"[Player] 으악! 데미지 {damageAmount} 받음! 남은 체력: {_currentHealth}");

        // 체력이 0 이하로 떨어지면 게임 오버
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 구독 해제
        if (_voiceInput != null)
        {
            _voiceInput.OnWordSpoken -= AttackTarget;
        }
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        if (PronunciationClient.Instance != null)
        {
            PronunciationClient.Instance.OnScoreReceived -= ExecuteVoiceAttack;
        }
    }

    private void Die()
    {
        Debug.Log("플레이어 사망! 게임 오버 요청!");
        
        // ⭐️ GameManager의 싱글톤 인스턴스를 통해 게임 상태를 게임 오버로 변경!
        GameManager.Instance.ChangeState(GameState.GameOver);
    }
}
