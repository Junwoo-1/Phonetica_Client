using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq; // 랜덤 섞기(OrderBy)를 위해 필요합니다.

public class CategorySelectionUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject categoryPanel; // 3개의 버튼이 들어있는 부모 패널
    public Button[] categoryButtons; // 3개의 카테고리 버튼 배열
    public TextMeshProUGUI[] buttonTexts; // 버튼 안의 텍스트 배열

    private List<CategoryEntry> _allCategories = new List<CategoryEntry>();

    void Start()
    {
        LoadCategories();
        ShowRandomCategories();
    }

    private void LoadCategories()
    {
        // ⭐️ 클라이언트 전용 JSON 파일을 읽어옵니다.
        string filePath = Path.Combine(Application.streamingAssetsPath, "WordBank.json");
        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            WordBankData data = JsonUtility.FromJson<WordBankData>(jsonContent);
            _allCategories = data.categories;
        }
    }

    public void ShowRandomCategories()
    {
        categoryPanel.SetActive(true);

        // ⭐️ LINQ를 활용해 전체 카테고리 중 무작위로 3개를 섞어서 뽑습니다.
        var shuffled = _allCategories.OrderBy(x => Random.value).Take(3).ToList();

        for (int i = 0; i < 3; i++)
        {
            if (i < shuffled.Count)
            {
                CategoryEntry selectedCategory = shuffled[i];
                buttonTexts[i].text = selectedCategory.categoryName;
                
                // 버튼에 달려있던 기존 이벤트를 싹 지우고, 이 카테고리를 선택하는 이벤트를 새로 달아줍니다.
                categoryButtons[i].onClick.RemoveAllListeners();
                categoryButtons[i].onClick.AddListener(() => OnCategorySelected(selectedCategory));
            }
        }
    }

    private void OnCategorySelected(CategoryEntry category)
    {
        categoryPanel.SetActive(false);

        // 1. EnemySpawner에게 "이 단어들만 스폰해!" 라고 전달합니다.
        EnemySpawner.Instance.SetWordList(category.words);
        
        // 2. (선택) UIManager를 통해 상단 화면에 "현재 카테고리: 숫자와 날짜" 텍스트를 띄웁니다.
        // UIManager.Instance.UpdateCategoryText(category.categoryName);

        // 3. ⭐️ 게임 시작! (원이 커지는 트랜지션 상태로 전환)
        //GameManager.Instance.ChangeState(GameState.Transition);
    }
}