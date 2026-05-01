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

    public Slider healthBarSlider;
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
    }

    private void Die()
    {
        Debug.Log("플레이어 사망! 게임 오버 요청!");
        
        // ⭐️ GameManager의 싱글톤 인스턴스를 통해 게임 상태를 게임 오버로 변경!
        GameManager.Instance.ChangeState(GameState.GameOver);
    }
}
