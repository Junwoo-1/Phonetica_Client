using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }
    public TextMeshProUGUI[] upgradeTexts;

    // 지금까지 해금한(선택한) 카테고리 이름들을 기억하는 바구니
    public static HashSet<string> UnlockedCategories = new HashSet<string>();

    // 현재 화면에 띄워진 선택지 데이터
    private List<CategoryEntry> _currentOptions = new List<CategoryEntry>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        ShowRandomCategories();
    }

    private void ShowRandomCategories()
    {
        _currentOptions.Clear();

        if (CategorySelectionUI.Instance == null) return;

        // 1. CategorySelectionUI에서 읽어온 전체 카테고리 목록을 가져옵니다.
        var allCategories = CategorySelectionUI.Instance.AllCategories;

        // 2. 전체 카테고리 중에서 '아직 내가 고르지 않은' 카테고리만 걸러냅니다.
        var availableCategories = allCategories.Where(c => !UnlockedCategories.Contains(c.categoryName)).ToList();

        // 3. 남은 것들 중에서 랜덤으로 3개를 뽑습니다.
        _currentOptions = availableCategories.OrderBy(x => Random.value).Take(3).ToList();

        // 4. 화면의 텍스트에 이름을 뿌려줍니다.
        for (int i = 0; i < upgradeTexts.Length; i++)
        {
            if (i < _currentOptions.Count && upgradeTexts[i] != null)
            {
                upgradeTexts[i].text = _currentOptions[i].categoryName;

                // 자기 자신만 켭니다!
                upgradeTexts[i].gameObject.SetActive(true);
            }
            else if (upgradeTexts[i] != null)
            {
                // 남는 빈칸 자기 자신만 끕니다!
                upgradeTexts[i].gameObject.SetActive(false);
            }
        }
    }

    public List<string> GetCurrentUpgrades()
    {
        // UIManager가 마이크에 장전할 수 있도록 카테고리 이름만 뽑아서 넘겨줍니다.
        return _currentOptions.Select(c => c.categoryName).ToList();
    }

    public void SelectUpgrade(string categoryName)
    {
        // 1. 유저가 말한 카테고리의 실제 데이터를 찾습니다.
        var selectedCategory = _currentOptions.FirstOrDefault(c => c.categoryName == categoryName);

        if (selectedCategory != null)
        {
            Debug.Log($"[LevelUpUI] 🎁 새 단어장 획득: {categoryName}");

            // 2. 해금 목록에 등록해서 다음 레벨업 때는 안 나오게 막습니다.
            UnlockedCategories.Add(categoryName);

            // 3. Spawner에게 이 카테고리의 단어들을 추가하라고 명령합니다!
            EnemySpawner.Instance.AddWordList(selectedCategory.words);
        }

        // 업그레이드가 끝났으니 다시 게임 시작!
        GameManager.Instance.ChangeState(GameState.Playing);
    }
}