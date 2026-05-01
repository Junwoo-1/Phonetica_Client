using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject enemyPrefab; // 찍어낼 적의 원본
    public float spawnInterval = 2.0f; // 몇 초마다 소환할지
    public float spawnRadius = 8.0f; // 플레이어 주변 몇 미터 밖에서 소환할지

    public Transform playerTransform;

    [Header("단어장 (Word Bank)")]
    // 인스펙터에서 마음대로 단어를 추가/수정할 수 있습니다.
    public List<string> wordBank = new List<string> { "사과", "바나나", "포도", "딸기", "수박", "오렌지", "키위", "망고" };
    
    // ⭐️ 핵심: 현재 화면에 나와 있어서 '사용 중인' 단어들을 보관하는 바구니
    private HashSet<string> _activeWords = new HashSet<string>();

    void Start()
    {
        // 게임 시작 시 무한 스폰 루프 시작
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        string wordToAssign = GetAvailableWord();
        if (string.IsNullOrEmpty(wordToAssign)) return;

        // 플레이어의 현재 위치를 기준으로 스폰 위치를 잡습니다.
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = playerTransform.position + new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();
        
        // ⭐️ 적을 초기화할 때 플레이어의 Transform도 같이 넘겨줍니다!
        enemyScript.Initialize(wordToAssign, playerTransform);
        
        _activeWords.Add(wordToAssign);
        enemyScript.OnDeath += HandleEnemyDeath;
    }

    private string GetAvailableWord()
    {
        // 전체 단어장 중에 현재 사용 중이지 않은 단어만 걸러냅니다.
        List<string> availableWords = new List<string>();
        foreach (string word in wordBank)
        {
            if (!_activeWords.Contains(word))
            {
                availableWords.Add(word);
            }
        }

        // 남은 단어가 없으면 null 반환
        if (availableWords.Count == 0) return null;

        // 남은 단어 중 랜덤으로 하나 선택
        int randomIndex = Random.Range(0, availableWords.Count);
        return availableWords[randomIndex];
    }

    // 적이 죽을 때 자동으로 실행될 함수
    private void HandleEnemyDeath(string deadWord, Enemy deadEnemy)
    {
        // 사용 중인 단어 목록에서 빼서 다시 쓸 수 있게 만듭니다.
        _activeWords.Remove(deadWord);
        
        // 메모리 누수 방지를 위해 이벤트 구독 해제
        deadEnemy.OnDeath -= HandleEnemyDeath;
    }
}