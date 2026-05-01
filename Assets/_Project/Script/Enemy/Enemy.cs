using UnityEngine;
using TMPro;
using System;

public class Enemy : MonoBehaviour
{
    public string myWord { get; private set; } 
    [SerializeField] private TextMeshPro _wordTextUI; 
    public event Action<string, Enemy> OnDeath; 

    [Header("이동 설정")]
    public float moveSpeed = 2.0f; // 적의 이동 속도
    private Transform _target; // 쫓아갈 목표 (플레이어)

    [Header("공격 설정")]
    public float collisionDamage = 10f; // 부딪혔을 때 플레이어에게 줄 데미지

    // ⭐️ Initialize 함수에 Transform 매개변수를 하나 추가합니다.
    public void Initialize(string assignedWord, Transform targetTransform)
    {
        myWord = assignedWord;
        _target = targetTransform; // 스포너가 넘겨준 플레이어 위치를 저장!
        
        if (_wordTextUI != null) _wordTextUI.text = myWord;
        gameObject.name = $"Enemy_{myWord}";
    }

    // ⭐️ 매 프레임마다 플레이어를 향해 이동합니다.
    void Update()
    {
        if (_target != null)
        {
            // 1. 방향 구하기: (목적지 - 내 위치) 후 길이를 1로 만듭니다(.normalized)
            Vector3 direction = (_target.position - transform.position).normalized;
            
            // 2. 내 위치를 방향 * 속도 * 시간만큼 이동시킵니다.
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"[{myWord}] 적이 {damage}의 데미지를 입었습니다!");
        Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 부딪힌 대상의 태그가 "Player"인지 확인합니다.
        if (other.CompareTag("Player"))
        {
            // 상대방(플레이어)의 스크립트를 가져와서 데미지를 입힙니다.
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(collisionDamage);
            }
            
            // 데미지를 주었으니 장렬하게 전사합니다.
            // (Die()를 호출하면 OnDeath 이벤트가 발생해 단어도 스포너에 정상 반납됩니다!)
            Die(); 
        }
    }
    private void Die()
    {
        Debug.Log($"[{myWord}] 사망!");
        OnDeath?.Invoke(myWord, this); 
        Destroy(gameObject); 
    }
}