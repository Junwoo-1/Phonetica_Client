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
    [Range(0, 360)] public float spawnAngleCenter = 90f; // 기본값 90 (위쪽에서 스폰)

    [Tooltip("부채꼴이 벌어지는 각도 (90이면 1/4 조각)")]
    [Range(0, 360)] public float spawnAngleSpread = 90f;

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
        _activeWords.Clear(); // 단어장이 바뀌었으니 현재 활성화된 단어 기록도 초기화합니다.
        Debug.Log($"[EnemySpawner] {selectedWords.Count}개의 단어가 세팅되었습니다!");

        // 기존에 돌고 있던 스폰 타이머가 있다면 끄고, 새롭게 시동을 겁니다!
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

        // 기존 360도 랜덤(Random.insideUnitCircle) 대신 부채꼴 각도 계산 로직을 사용합니다.
        float halfSpread = spawnAngleSpread / 2f;

        // 중심 각도를 기준으로 -절반 ~ +절반 범위 안에서 랜덤한 각도를 하나 뽑습니다.
        float randomAngle = Random.Range(spawnAngleCenter - halfSpread, spawnAngleCenter + halfSpread);

        // 뽑힌 각도(도 단위)를 라디안(Radian)으로 변환합니다. (Mathf 삼각함수는 라디안을 사용함)
        float angleRad = randomAngle * Mathf.Deg2Rad;

        // 코사인(Cos)은 X축, 사인(Sin)은 Y축 방향을 나타냅니다. 이를 통해 방향 벡터를 만듭니다.
        Vector2 randomDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        // 플레이어 위치 + (계산된 방향 * 거리) 위치에 적을 스폰합니다.
        Vector3 spawnPos = playerTransform.position + new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();

        enemyScript.Initialize(selectedEntry.word, selectedEntry.pronunciation, playerTransform);

        _activeWords.Add(selectedEntry.word);
        enemyScript.OnDeath += HandleEnemyDeath;
    }
    private WordEntry GetAvailableWordEntry(string excludeWord = "")
    {
        List<WordEntry> availableEntries = new List<WordEntry>();
        foreach (WordEntry entry in _wordBank)
        {
            // 화면에 이미 떠있는 단어이거나, '방금까지 내가 쓰던 단어(excludeWord)'면 후보에서 제외!
            if (!_activeWords.Contains(entry.word) && entry.word != excludeWord)
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
        // 1. 기존 단어(oldWord)를 '제외'하고 남은 단어 중에서 새 단어를 뽑습니다.
        WordEntry newEntry = GetAvailableWordEntry(oldWord);

        // 2. 만약 바꿀 수 있는 다른 단어가 아예 없다면, 교체하지 않고 조용히 리턴합니다.
        if (newEntry == null)
        {
            return;
        }

        // 3. 다른 단어가 존재한다면 기존 단어를 반납하고 새 단어로 교체합니다.
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
        // 플레이어가 할당되지 않았다면 그리지 않음
        if (playerTransform == null) return;

        // 시작 각도 계산 (중심각 - 절반)
        float startAngle = spawnAngleCenter - (spawnAngleSpread / 2f);

        // 각도를 라디안으로 변환하여 시작 방향 벡터 생성
        Vector3 startDir = new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad), 0);

        // 끝 각도를 라디안으로 변환하여 끝 방향 벡터 생성
        float endAngle = spawnAngleCenter + (spawnAngleSpread / 2f);
        Vector3 endDir = new Vector3(Mathf.Cos(endAngle * Mathf.Deg2Rad), Mathf.Sin(endAngle * Mathf.Deg2Rad), 0);

        // 1. 부채꼴 내부를 반투명한 붉은색으로 채우기
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.2f); // 빨간색, 투명도 20%
        UnityEditor.Handles.DrawSolidArc(playerTransform.position, Vector3.forward, startDir, spawnAngleSpread, spawnRadius);

        // 2. 부채꼴 외곽선(호) 뚜렷하게 그리기
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireArc(playerTransform.position, Vector3.forward, startDir, spawnAngleSpread, spawnRadius);

        // 3. 부채꼴의 양 끝 직선(반지름) 그리기
        Gizmos.color = Color.red;
        Gizmos.DrawRay(playerTransform.position, startDir * spawnRadius);
        Gizmos.DrawRay(playerTransform.position, endDir * spawnRadius);
    }
#endif
    //여기까지입니다! (클래스를 닫는 마지막 } 직전에 넣으세요)
}