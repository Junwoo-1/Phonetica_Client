using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CategorySelectionUI : MonoBehaviour
{
    // UIManager가 쉽게 부를 수 있도록 싱글톤 처리
    public static CategorySelectionUI Instance { get; private set; }

    [Header("UI 연결 (버튼 대신 텍스트만 필요합니다)")]
    public TextMeshProUGUI[] categoryTexts;

    private List<CategoryEntry> _allCategories = new List<CategoryEntry>();

    // 현재 화면에 띄워진 3개의 카테고리
    public List<CategoryEntry> CurrentDisplayedCategories { get; private set; } = new List<CategoryEntry>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        LoadCategories();
    }
    private void OnEnable()
    {
        // Awake에서 이미 데이터를 꽉 채워놨기 때문에, 이제 켜질 때마다 안전하게 글자를 바꿉니다.
        if (_allCategories != null && _allCategories.Count > 0)
        {
            ShowRandomCategories();
        }
    }

    private void LoadCategories()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "WordBank.json");
        Debug.Log($"[CategoryUI] 파일 찾기 시도 경로: {filePath}");

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            WordBankData data = JsonUtility.FromJson<WordBankData>(jsonContent);

            // 데이터 파싱이 성공적으로 되었는지 꼼꼼하게 검사합니다.
            if (data != null && data.categories != null && data.categories.Count > 0)
            {
                _allCategories = data.categories;
                Debug.Log($"[CategoryUI] 카테고리 로드 대성공! 총 {_allCategories.Count}개의 카테고리를 찾았습니다.");
            }
            else
            {
                // 파일은 있는데 파싱을 실패한 경우
                Debug.LogError("[CategoryUI] 파일은 찾았지만 데이터를 읽어오지 못했습니다! \n 1. JsonDataStructure.cs 안의 클래스들에 [Serializable] 속성이 붙어있는지 확인하세요.\n 2. JSON 구조에 오타가 없는지 확인하세요.");
            }
        }
        else
        {
            // 파일을 아예 찾지 못한 경우
            Debug.LogError($"[CategoryUI] JSON 파일을 찾을 수 없습니다! StreamingAssets 폴더 안에 'WordBank.json' 파일이 정확히 있는지 확인해 주세요.");
        }
    }

    public void ShowRandomCategories()
    {
        CurrentDisplayedCategories = _allCategories.OrderBy(x => Random.value).Take(3).ToList();

        for (int i = 0; i < 3; i++)
        {
            if (i < CurrentDisplayedCategories.Count)
            {
                categoryTexts[i].text = CurrentDisplayedCategories[i].categoryName;
            }
        }
    }

    // UIManager가 음성 인식 결과를 던져주면 실행될 함수
    public void SelectCategoryByName(string categoryName)
    {
        Debug.Log($"[CategoryUI] 검색 시도: '{categoryName}'");

        // 공백을 무시하고 정확히 일치하는 카테고리를 찾습니다.
        CategoryEntry selectedCategory = CurrentDisplayedCategories.FirstOrDefault(c => c.categoryName.Trim() == categoryName.Trim());

        if (selectedCategory != null)
        {
            Debug.Log($"[CategoryUI] '{categoryName}' 카테고리 일치 성공! 게임을 시작합니다.");

            LevelUpUI.UnlockedCategories.Add(categoryName);

            // 안전장치: EnemySpawner가 비활성화 상태라 못 찾는 경우를 대비
            if (EnemySpawner.Instance == null)
            {
                Debug.LogError("[CategoryUI] EnemySpawner.Instance가 null입니다! \nEnemySpawner 스크립트가 붙은 오브젝트가 꺼져있는 GamePanel 안에 들어있는지 확인하세요.");
            }
            else
            {
                EnemySpawner.Instance.SetWordList(selectedCategory.words);
            }

            UIManager.Instance.SwitchBasePanel(UIManager.Instance.gamePanel);
            GameManager.Instance.ChangeState(GameState.Playing);
        }
        else
        {
            Debug.LogError($"[CategoryUI] '{categoryName}'와(과) 일치하는 카테고리를 찾지 못했습니다.");
        }
    }

    // 서버로 보낼 '현재 떠 있는 카테고리 이름들'을 반환하는 함수
    public List<string> GetCurrentCategoryNames()
    {
        return CurrentDisplayedCategories.Select(c => c.categoryName).ToList();
    }
}