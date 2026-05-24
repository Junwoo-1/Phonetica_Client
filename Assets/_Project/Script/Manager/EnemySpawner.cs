using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; // 파일 읽기를 위해 필요

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject enemyPrefab;
    public float spawnInterval = 2.0f;
    public float spawnRadius = 8.0f;
    public Transform playerTransform;

    // ⭐️ 인스펙터에서 직접 넣는 대신 코드로 채워질 리스트
    private List<WordEntry> _wordBank = new List<WordEntry>();
    private HashSet<string> _activeWords = new HashSet<string>();

    void Start()
    {
        LoadWordBankFromJson(); // ⭐️ 시작 시 JSON 로드
        StartCoroutine(SpawnRoutine());
    }

    private void LoadWordBankFromJson()
    {
        // StreamingAssets 폴더 경로 설정
        string filePath = Path.Combine(Application.streamingAssetsPath, "WordBank.json");

        if (File.Exists(filePath))
        {
            // 1. 파일 내용 읽기
            string jsonContent = File.ReadAllText(filePath);

            // 2. JSON 파싱
            WordBankData data = JsonUtility.FromJson<WordBankData>(jsonContent);

            // 3. 리스트 저장
            _wordBank = data.wordList;
            Debug.Log($"[EnemySpawner] {_wordBank.Count}개의 단어를 성공적으로 로드했습니다.");
        }
        else
        {
            Debug.LogError($"[EnemySpawner] JSON 파일을 찾을 수 없습니다: {filePath}");
        }
    }

    private void SpawnEnemy()
    {
        WordEntry selectedEntry = GetAvailableWordEntry();
        if (selectedEntry == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = playerTransform.position + new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();

        // 이중 데이터 구조(표시 단어, 발음 단어)를 넘겨줍니다.
        enemyScript.Initialize(selectedEntry.word, selectedEntry.pronunciation, playerTransform);

        _activeWords.Add(selectedEntry.word);
        enemyScript.OnDeath += HandleEnemyDeath;
    }

    // GetAvailableWordEntry 및 HandleEnemyDeath 로직은 _wordBank 변수명을 제외하고 이전과 동일합니다.
    private WordEntry GetAvailableWordEntry()
    {
        List<WordEntry> availableEntries = new List<WordEntry>();
        foreach (WordEntry entry in _wordBank)
        {
            if (!_activeWords.Contains(entry.word))
            {
                availableEntries.Add(entry);
            }
        }
        if (availableEntries.Count == 0) return null;
        int randomIndex = Random.Range(0, availableEntries.Count);
        return availableEntries[randomIndex];
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
}