using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
public class Enemy : MonoBehaviour
{
    public string displayText { get; private set; }
    public string matchText { get; private set; }

    [Header("체력 설정")]
    public float maxHP = 100f;
    private float currentHP;
    private bool _isDead = false; // 죽음 처리 중인지 확인하는 방어막

    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshPro _wordTextUI;
    public event Action<string, Enemy> OnDeath;

    [Header("이동 설정")]
    public float moveSpeed = 2.0f;
    private Transform _target;

    [Header("공격 설정")]
    public float collisionDamage = 10f;

    [Header("사망 이벤트")]
    public float expReward = 20f;
    public GameObject deathEffectPrefab;

    public void Initialize(string visualWord, string pronunciation, Transform targetTransform)
    {
        _target = targetTransform;
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        UpdateWord(visualWord, pronunciation);
    }

    public void UpdateWord(string visualWord, string pronunciation)
    {
        // ⭐️ 죽는 중이라면 단어를 갱신하지 않습니다.
        if (_isDead) return;

        displayText = visualWord;
        matchText = pronunciation;

        if (_wordTextUI != null) _wordTextUI.text = displayText;
        gameObject.name = $"Enemy_{displayText}";
    }

    // ⭐️ 생존 여부를 bool로 반환하도록 수정
    public bool TakeDamage(float damage)
    {
        if (_isDead) return false;

        currentHP -= damage;
        Debug.Log($"[{displayText}] {damage} 데미지! 남은 HP: {currentHP}");

        if (hpSlider != null) hpSlider.value = currentHP;

        if (currentHP <= 0)
        {
            _isDead = true; // 즉시 죽음 확정 플래그를 세웁니다.
            Die();
            return false; // 죽었음
        }

        return true; // 살아있음
    }

    void Update()
    {

        if (_target != null && !_isDead) // 죽는 중에는 이동 정지
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_isDead)
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null) playerScript.TakeDamage(collisionDamage);
            _isDead = true;
            Die();
        }
    }

    private void Die()
    {
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (_target != null)
        {
            Player playerScript = _target.GetComponent<Player>();
            if (playerScript != null) playerScript.AddExp(expReward);
        }

        // 스포너에게 죽은 단어("포도")를 반환합니다.
        OnDeath?.Invoke(displayText, this);
        Destroy(gameObject);
    }
}