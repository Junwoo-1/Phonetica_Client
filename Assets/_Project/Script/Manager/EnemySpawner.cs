using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class EnemySpawner : MonoBehaviour
{
    // ⭐️ 다른 스크립트에서 쉽게 접근할 수 있도록 싱글톤 인스턴스 생성
    public static EnemySpawner Instance { get; private set; }

    [Header("스폰 설정")]
    public GameObject enemyPrefab;
    public float spawnInterval = 2.0f;
    public float spawnRadius = 8.0f;
    public Transform playerTransform;

    private List<WordEntry> _wordBank = new List<WordEntry>();
    private HashSet<string> _activeWords = new HashSet<string>();

    // ⭐️ VoiceRecorder에서 긁어갈 수 있도록 프로퍼티 개방
    public HashSet<string> ActiveWords => _activeWords;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadWordBankFromJson();
        StartCoroutine(SpawnRoutine());
    }

    private void LoadWordBankFromJson()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "WordBank.json");

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            WordBankData data = JsonUtility.FromJson<WordBankData>(jsonContent);
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

        enemyScript.Initialize(selectedEntry.word, selectedEntry.pronunciation, playerTransform);

        _activeWords.Add(selectedEntry.word);
        enemyScript.OnDeath += HandleEnemyDeath;
    }

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
    public void ChangeEnemyWord(Enemy enemy, string oldWord)
    {
        // 1. 기존 단어 반납
        _activeWords.Remove(oldWord);

        // 2. 새 단어 뽑기
        WordEntry newEntry = GetAvailableWordEntry();

        if (newEntry == null)
        {
            _activeWords.Add(oldWord);
            return;
        }

        // 3. 새 단어 등록
        _activeWords.Add(newEntry.word);

        // 4. Initialize 대신 UpdateWord를 호출하여 체력을 보존합니다!
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
}