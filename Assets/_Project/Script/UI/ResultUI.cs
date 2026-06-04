using UnityEngine;
using TMPro;
using System.Linq;


public class ResultUI : MonoBehaviour
{

    // 승리/패배 여부를 임시로 적어둘 공용 메모장
    public static bool IsClearData = false;

    [Header("공통 데이터 표시 텍스트")]
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI categoriesText;
    public TextMeshProUGUI accuracyText;

    [Header("조건부 데코레이션 (On/Off)")]
    public TextMeshProUGUI titleText;
    public GameObject winDecorationGroup;
    public GameObject loseDecorationGroup;

    // 패널이 화면에 '켜질 때' 자동으로 메모장을 읽고 텍스트를 갱신합니다!
    private void OnEnable()
    {
        ShowResult(IsClearData);
    }

    // 기존의 ShowResult 함수는 이름은 그대로 두고, 안의 로직만 유지합니다.
    private void ShowResult(bool isClear) // public에서 private으로 바꿔도 됩니다.
    {
        Player player = FindObjectOfType<Player>();
        int kills = player != null ? player.killCount : 0;

        float avgScore = 0f;
        if (player != null && player.validAttackCount > 0)
        {
            avgScore = player.totalAccuracyScore / player.validAttackCount;
        }

        string categories = "없음";
        if (LevelUpUI.UnlockedCategories.Count > 0)
        {
            categories = string.Join(", ", LevelUpUI.UnlockedCategories);
        }

        if (killCountText != null) killCountText.text = $"처치한 단어 몬스터: {kills} 마리";
        if (categoriesText != null) categoriesText.text = $"학습한 주제: {categories}";
        if (accuracyText != null) accuracyText.text = $"평균 발음 정확도: {avgScore:F1} 점";

        if (titleText != null) titleText.text = isClear ? "게임 클리어!!" : "게임 오버...";

        if (winDecorationGroup != null) winDecorationGroup.SetActive(isClear);
        if (loseDecorationGroup != null) loseDecorationGroup.SetActive(!isClear);
    }
}