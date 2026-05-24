using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private IVoiceInput _voiceInput;
    public float baseDamage = 100f; // 기본 데미지 상향 (점수 비율 반영 대비)

    [Header("체력 시스템")]
    public float maxHealth = 100f;
    private float _currentHealth;

    [Header("경험치 시스템")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float maxExp = 100f;

    public Slider healthBarSlider;
    public Slider expBarSlider;

    void Start()
    {
        _voiceInput = FindObjectOfType<MockKeyboardInput>();
        if (_voiceInput != null)
        {
            _voiceInput.OnWordSpoken += AttackTarget;
        }

        _currentHealth = maxHealth;
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = _currentHealth;
        }

        if (expBarSlider != null)
        {
            expBarSlider.maxValue = maxExp;
            expBarSlider.value = currentExp;
        }

        // ⭐️ 네트워크 매니저로부터 발음 분석 결과 수신 시 실행
        if (PronunciationClient.Instance != null)
        {
            PronunciationClient.Instance.OnScoreReceived += ExecuteVoiceAttack;
        }
    }

    // ⭐️ [핵심] 서버의 실제 발음 분석 결과를 바탕으로 타격 수행
    private void ExecuteVoiceAttack(ScorePayload payload)
    {
        string recognizedWord = payload.recognized_word;
        if (string.IsNullOrEmpty(recognizedWord)) return;

        Enemy[] targets = FindObjectsOfType<Enemy>();
        bool isHit = false;

        foreach (Enemy enemy in targets)
        {
            if (enemy.displayText == recognizedWord)
            {
                // [상태 A: 단어 인식 성공]
                // 1. 데미지 처리
                float damage = baseDamage * (payload.overall_score / 100f);
                enemy.TakeDamage(damage);
                isHit = true;

                // 2. 정밀 피드백 처리 (단어가 일치할 때만 서버의 점수가 유효함!)
                if (payload.problem_jamos != null && payload.problem_jamos.Length > 0)
                {
                    Debug.Log($"[발음 교정] {enemy.displayText} 타격! 🚨주의 자모: {string.Join(", ", payload.problem_jamos)}");
                }
                else
                {
                    Debug.Log($"[발음 완벽] {enemy.displayText} 타격! (점수: {payload.overall_score})");
                }
            }
        }

        // [상태 B: 단어 인식 실패 (다뽑기 등 엉뚱한 단어로 인식된 경우)]
        if (!isHit)
        {
            // ⭐️ 여기에 "오인식 피드백" 로직을 넣습니다!
            // 서버의 점수(overall_score)는 무시하고, '어떻게 들렸는지'만 알려줍니다.

            Debug.LogWarning($"❌ 공격 실패! 플레이어님, 방금 🗣️'{recognizedWord}'(이)라고 발음하셨습니다.");

            // TODO: 유니티 UI로 화면 중앙이나 플레이어 머리 위에 띄워주기
            // ShowFloatingText($"듣기 실패: {recognizedWord}"); 
        }
    }

    // ⭐️ [디버깅용] 키보드 입력 시에도 matchText를 기준으로 동작하도록 수정
    private void AttackTarget(VoiceData data)
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in allEnemies)
        {
            // 가짜 입력기로 테스트할 때도 발음(matchText) 기준으로 검사합니다.
            if (enemy.matchText == data.word)
            {
                float finalDamage = baseDamage * (data.accuracy + data.pitchScore);
                enemy.TakeDamage(finalDamage);
                return;
            }
        }
        Debug.Log("일치하는 발음 데이터를 가진 적이 없습니다!");
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
        if (_voiceInput != null)
        {
            _voiceInput.OnWordSpoken -= AttackTarget;
        }
        if (PronunciationClient.Instance != null)
        {
            PronunciationClient.Instance.OnScoreReceived -= ExecuteVoiceAttack;
        }
    }

    private void Die()
    {
        Debug.Log("플레이어 사망! 게임 오버!");
        GameManager.Instance.ChangeState(GameState.GameOver);
    }
}
