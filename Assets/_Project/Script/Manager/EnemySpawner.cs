using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("스폰 설정")]
    public GameObject enemyPrefab;
    public float spawnInterval = 2.0f;
    public float spawnRadius = 8.0f;
    public Transform playerTransform;

    [Header("부채꼴(피자 모양) 스폰 설정")]
    [Tooltip("0: 오른쪽, 90: 위쪽, 180: 왼쪽, 270: 아래쪽")]
    [Range(0, 360)] public float spawnAngleCenter = 90f;

    [Tooltip("부채꼴이 벌어지는 각도 (90이면 1/4 조각)")]
    [Range(0, 360)] public float spawnAngleSpread = 90f;


    [Header("스폰 알고리즘")]
    private string _lastSpawnedWord = ""; // 직전에 소환된 단어를 기억하는 변수

    // ⭐️ 두 단어가 얼마나 다른지 계산하는 마법의 함수 (유니티 Mathf로 안전하게 수정)
    private int GetLevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        int[,] d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                // 유니티에 내장된 Mathf.Min을 사용하여 System 에러를 방지합니다.
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }
        return d[a.Length, b.Length];
    }


    private List<WordEntry> _wordBank = new List<WordEntry>();
    private HashSet<string> _activeWords = new HashSet<string>();

    public HashSet<string> ActiveWords => _activeWords;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetWordList(List<WordEntry> selectedWords)
    {
        _wordBank = selectedWords;
        _activeWords.Clear();
        Debug.Log($"[EnemySpawner] {selectedWords.Count}개의 단어가 세팅되었습니다!");

        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }

    public void AddWordList(List<WordEntry> newWords)
    {
        _wordBank.AddRange(newWords);
        Debug.Log($"[EnemySpawner] 📚 카테고리 추가됨! 현재 스폰 가능한 단어 풀: 총 {_wordBank.Count}개");
    }

    private void SpawnEnemy()
    {
        WordEntry selectedEntry = GetAvailableWordEntry();
        if (selectedEntry == null) return;

        float halfSpread = spawnAngleSpread / 2f;
        float randomAngle = Random.Range(spawnAngleCenter - halfSpread, spawnAngleCenter + halfSpread);
        float angleRad = randomAngle * Mathf.Deg2Rad;

        Vector2 randomDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        Vector3 spawnPos = playerTransform.position + new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();

        enemyScript.Initialize(selectedEntry.word, selectedEntry.pronunciation, playerTransform);

        _activeWords.Add(selectedEntry.word);
        enemyScript.OnDeath += HandleEnemyDeath;
    }

    // ⭐️ [NEW] Best of 3 알고리즘이 적용된 단어 뽑기 함수
    private WordEntry GetAvailableWordEntry(string excludeWord = "")
    {
        List<WordEntry> availableEntries = new List<WordEntry>();
        foreach (WordEntry entry in _wordBank)
        {
            if (!_activeWords.Contains(entry.word) && entry.word != excludeWord)
            {
                availableEntries.Add(entry);
            }
        }

        // 스폰할 수 있는 단어가 없다면 포기
        if (availableEntries.Count == 0) return null;

        // 첫 번째 스폰이거나 비교할 단어가 없을 때는 그냥 랜덤으로 하나 줍니다.
        if (string.IsNullOrEmpty(_lastSpawnedWord))
        {
            WordEntry randomEntry = availableEntries[Random.Range(0, availableEntries.Count)];
            _lastSpawnedWord = randomEntry.word;
            return randomEntry;
        }

        WordEntry bestEntry = null;
        int maxDistance = -1;

        // ⭐️ 후보 3개를 뽑아서, 직전 단어(_lastSpawnedWord)와 가장 거리가 먼(안 비슷한) 단어를 찾습니다!
        int candidateCount = Mathf.Min(3, availableEntries.Count);
        for (int i = 0; i < candidateCount; i++)
        {
            WordEntry candidate = availableEntries[Random.Range(0, availableEntries.Count)];
            int dist = GetLevenshteinDistance(_lastSpawnedWord, candidate.word);

            // 거리가 더 멀다면 최고 후보로 갱신!
            if (dist > maxDistance)
            {
                maxDistance = dist;
                bestEntry = candidate;
            }
        }

        // 찾아낸 가장 안전한 단어를 다음 비교를 위해 기억하고 반환합니다.
        _lastSpawnedWord = bestEntry.word;
        return bestEntry;
    }

    public void ChangeEnemyWord(Enemy enemy, string oldWord)
    {
        WordEntry newEntry = GetAvailableWordEntry(oldWord);

        if (newEntry == null) return;

        _activeWords.Remove(oldWord);
        _activeWords.Add(newEntry.word);
        enemy.UpdateWord(newEntry.word, newEntry.pronunciation);
    }

    private void HandleEnemyDeath(string deadWord, Enemy deadEnemy)
    {
        _activeWords.Remove(deadWord);
        deadEnemy.OnDeath -= HandleEnemyDeath;
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (_wordBank.Count > 0) SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        float startAngle = spawnAngleCenter - (spawnAngleSpread / 2f);
        Vector3 startDir = new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad), 0);

        float endAngle = spawnAngleCenter + (spawnAngleSpread / 2f);
        Vector3 endDir = new Vector3(Mathf.Cos(endAngle * Mathf.Deg2Rad), Mathf.Sin(endAngle * Mathf.Deg2Rad), 0);

        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.2f);
        UnityEditor.Handles.DrawSolidArc(playerTransform.position, Vector3.forward, startDir, spawnAngleSpread, spawnRadius);

        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireArc(playerTransform.position, Vector3.forward, startDir, spawnAngleSpread, spawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(playerTransform.position, startDir * spawnRadius);
        Gizmos.DrawRay(playerTransform.position, endDir * spawnRadius);
    }
#endif
}