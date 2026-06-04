using UnityEngine;
using TMPro;
using System.Linq;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [Header("공통 데이터 표시 텍스트")]
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI categoriesText;
    public TextMeshProUGUI accuracyText;

    [Header("조건부 데코레이션 (On/Off)")]
    public TextMeshProUGUI titleText;      // "게임 클리어!" vs "게임 오버..."
    public GameObject winDecorationGroup;  // 승리 시 켤 오브젝트들 (폭죽, 밝은 배경 등)
    public GameObject loseDecorationGroup; // 패배 시 켤 오브젝트들 (해골, 붉은 배경 등)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 게임이 끝날 때 호출되는 핵심 함수 (isClear가 true면 승리, false면 패배)
    public void ShowResult(bool isClear)
    {
        // 1. Player 스크립트를 찾아서 통계 데이터를 가져옵니다.
        Player player = FindObjectOfType<Player>();
        int kills = player != null ? player.killCount : 0;
        
        float avgScore = 0f;
        if (player != null && player.validAttackCount > 0)
        {
            avgScore = player.totalAccuracyScore / player.validAttackCount;
        }

        // 2. LevelUpUI의 static 바구니에서 학습한 카테고리들을 꺼내옵니다.
        string categories = "없음";
        if (LevelUpUI.UnlockedCategories.Count > 0)
        {
            categories = string.Join(", ", LevelUpUI.UnlockedCategories);
        }

        // 3. 화면의 공통 텍스트 UI에 데이터를 예쁘게 뿌려줍니다.
        if (killCountText != null) killCountText.text = $"처치한 단어 몬스터: {kills} 마리";
        if (categoriesText != null) categoriesText.text = $"학습한 주제: {categories}";
        if (accuracyText != null) accuracyText.text = $"평균 발음 정확도: {avgScore:F1} 점";

        // 4. ⭐️ 기획하신 조건부 On/Off 연출!
        if (titleText != null) titleText.text = isClear ? "게임 클리어!" : "게임 오버...";
        
        if (winDecorationGroup != null) winDecorationGroup.SetActive(isClear);
        if (loseDecorationGroup != null) loseDecorationGroup.SetActive(!isClear);

        // 5. 자기 자신(결과 패널)을 화면에 켭니다.
        gameObject.SetActive(true);
    }
}