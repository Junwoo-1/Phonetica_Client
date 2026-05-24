using UnityEngine;
using TMPro;

public class SyllableUI : MonoBehaviour
{
    public TextMeshProUGUI choText;
    public TextMeshProUGUI jungText1; // 첫 번째 모음 (ㅗ, ㅓ 등)
    public TextMeshProUGUI jungText2; // 두 번째 모음 (ㅏ, ㅣ 등 - 이중모음에만 사용)
    public TextMeshProUGUI jongText;

    [Header("디테일 간격 세팅")]
    public float verticalGap = 20f;      // 위아래 간격 (포, 두)
    public float horizontalGap = 25f;    // 좌우 간격 (바, 나)
    public float doubleVowelGap = 8f;    // ㅔ, ㅐ 등에서 ㅓ와 ㅣ 사이의 미세 간격
    public float jongOffset = 35f;       // 받침이 밑으로 내려가는 정도
    public float jongPushUp = 10f;       // 받침이 있을 때 전체 글자가 위로 올라가는 정도

    private string bottomVowels = "ㅗㅛㅜㅠㅡ";

    public void SetSyllable(string cho, string jung1, string jung2, string jong, Color choC, Color j1C, Color j2C, Color jongC)
    {
        // 1. 텍스트 및 개별 색상 적용
        choText.text = cho; choText.color = choC;
        jungText1.text = jung1; jungText1.color = j1C;
        jungText2.text = jung2; jungText2.color = j2C;
        jongText.text = jong; jongText.color = jongC;

        RectTransform choR = choText.GetComponent<RectTransform>();
        RectTransform j1R = jungText1.GetComponent<RectTransform>();
        RectTransform j2R = jungText2.GetComponent<RectTransform>();
        RectTransform jongR = jongText.GetComponent<RectTransform>();

        bool hasJong = !string.IsNullOrEmpty(jong);
        float yOffset = hasJong ? jongPushUp : 0f;

        bool hasJung2 = !string.IsNullOrEmpty(jung2);
        bool isJ1Bottom = !string.IsNullOrEmpty(jung1) && bottomVowels.Contains(jung1);

        // 2. 한글 구조별 4단 자동 정렬 로직
        if (isJ1Bottom && hasJung2)
        {
            // [혼합형] 과, 줘 (ㅗ+ㅏ) -> 자음은 좌상단, 모음1은 하단, 모음2는 우측
            choR.anchoredPosition = new Vector2(-horizontalGap / 2, yOffset + verticalGap / 2);
            j1R.anchoredPosition = new Vector2(-horizontalGap / 2, yOffset - verticalGap);
            j2R.anchoredPosition = new Vector2(horizontalGap, yOffset);
        }
        else if (isJ1Bottom && !hasJung2)
        {
            // [수직형] 고, 두 (ㅗ) -> 자음 상단, 모음 하단
            choR.anchoredPosition = new Vector2(0, yOffset + verticalGap);
            j1R.anchoredPosition = new Vector2(0, yOffset - verticalGap);
            j2R.anchoredPosition = Vector2.zero;
        }
        else if (!isJ1Bottom && hasJung2)
        {
            // [수평 이중모음] 에, 애 (ㅓ+ㅣ) -> ㅓ와 ㅣ를 나란히 배치
            choR.anchoredPosition = new Vector2(-horizontalGap, yOffset);
            j1R.anchoredPosition = new Vector2(horizontalGap - doubleVowelGap, yOffset);
            j2R.anchoredPosition = new Vector2(horizontalGap + doubleVowelGap, yOffset);
        }
        else
        {
            // [수평 단일모음] 바, 나 (ㅏ)
            choR.anchoredPosition = new Vector2(-horizontalGap, yOffset);
            j1R.anchoredPosition = new Vector2(horizontalGap, yOffset);
            j2R.anchoredPosition = Vector2.zero;
        }

        // 3. 받침 정렬
        if (hasJong) jongR.anchoredPosition = new Vector2(0, -jongOffset);
    }
}