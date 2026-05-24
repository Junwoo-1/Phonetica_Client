using UnityEngine;
using TMPro;
using System;

public class Enemy : MonoBehaviour
{
    // ⭐️ 1. 시각적으로 보일 텍스트와 판정에 쓸 발음 데이터를 분리합니다.
    public string displayText { get; private set; }
    public string matchText { get; private set; }

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

    // ⭐️ 2. Initialize에서 실제 단어와 발음 정보를 모두 받도록 확장합니다.
    public void Initialize(string visualWord, string pronunciation, Transform targetTransform)
    {
        displayText = visualWord;   // 예: "닭볶이"
        matchText = pronunciation;   // 예: "닥뽀끼" (G2P 적용 결과)
        _target = targetTransform;

        if (_wordTextUI != null) _wordTextUI.text = displayText;
        gameObject.name = $"Enemy_{displayText}";
    }

    void Update()
    {
        if (_target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"[{displayText}] 적이 {damage}의 데미지를 입었습니다!");
        Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(collisionDamage);
            }
            Die();
        }
    }

    private void Die()
    {
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        if (_target != null)
        {
            Player playerScript = _target.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.AddExp(expReward);
            }
        }

        // ⭐️ 3. 스포너에는 다시 표시용 단어(displayText)를 반납합니다.
        OnDeath?.Invoke(displayText, this);
        Destroy(gameObject);
    }
}