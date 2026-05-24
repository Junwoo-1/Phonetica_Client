using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq; // 데이터 그룹화를 위해 추가

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 패널들")]
    public GameObject gameOverPanel;
    public GameObject levelUpPanel;

    [Header("음성 인식 UI")]
    public TextMeshProUGUI statusText;

    [Header("조립식 피드백 설정")]
    public Transform wordContainer;    // 단어 전체가 담길 부모 (Horizontal Layout Group)
    public GameObject syllablePrefab; // ⭐️ 개별 음절(글자) 프리팹
    public TextMeshProUGUI feedbackTitleText;
    public float feedbackDisplayTime = 2.0f;

    // 가로형 모음 (자음 아래에 붙는 모음들)
    private string horizontalVowels = "ㅗㅛㅜㅠㅡ";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        if (PronunciationClient.Instance != null)
            PronunciationClient.Instance.OnStatusUpdated += UpdateStatusText;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        if (PronunciationClient.Instance != null)
            PronunciationClient.Instance.OnStatusUpdated -= UpdateStatusText;
    }

    // ⭐️ 음절 단위 피드백 핵심 로직
    public void ShowSyllableFeedback(string title, List<JamoScoreInfo> detailedJamos)
    {
        if (wordContainer == null || syllablePrefab == null) return;

        // 1. 기존 UI 제거
        foreach (Transform child in wordContainer) Destroy(child.gameObject);
        if (feedbackTitleText != null) feedbackTitleText.text = title;

        // 2. 서버 데이터를 음절(syl) 인덱스별로 그룹화
        var groupedBySyllable = detailedJamos.GroupBy(j => j.syl).OrderBy(g => g.Key);

        foreach (var group in groupedBySyllable)
        {
            // 한 음절 상자 생성
            GameObject syllableObj = Instantiate(syllablePrefab, wordContainer);

            // 음절 내 자음/모음 데이터 추출
            var jamosInSyllable = group.ToList();
            var onset = jamosInSyllable.FirstOrDefault(j => j.pos == "onset");
            var nucleus = jamosInSyllable.FirstOrDefault(j => j.pos == "nucleus");
            var coda = jamosInSyllable.FirstOrDefault(j => j.pos == "coda");

            // 3. 음절 내 자모 배치 (SyllableUI 컴포넌트 사용 권장이나 여기서는 직접 제어 예시)
            SetupSyllable(syllableObj, onset, nucleus, coda);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(wordContainer.GetComponent<RectTransform>());

        StopCoroutine("ClearFeedbackRoutine");
        StartCoroutine("ClearFeedbackRoutine");
    }

    private void SetupSyllable(GameObject block, JamoScoreInfo cho, JamoScoreInfo jung, JamoScoreInfo jong)
    {
        SyllableUI syllableUI = block.GetComponent<SyllableUI>();
        if (syllableUI == null) return;

        string choStr = cho != null ? cho.@char : "";
        string jongStr = jong != null ? jong.@char : "";
        string jungStr1 = "";
        string jungStr2 = "";

        // 자모별 개별 색상 추출
        Color choColor = cho != null ? GetColor(cho.score) : Color.white;
        Color jungColor = jung != null ? GetColor(jung.score) : Color.white;
        Color jongColor = jong != null ? GetColor(jong.score) : Color.white;

        if (jung != null)
        {
            // "ㅘ" -> splitVowels[0] = "ㅗ", splitVowels[1] = "ㅏ"
            string[] splitVowels = JamoSplitter.Split(jung.@char);
            jungStr1 = splitVowels[0];
            if (splitVowels.Length > 1)
            {
                jungStr2 = splitVowels[1];
            }
        }

        // 추출한 데이터를 SyllableUI로 전달 (이중모음은 서버에서 하나의 모음 점수로 오므로 jungColor를 공통 적용)
        syllableUI.SetSyllable(choStr, jungStr1, jungStr2, jongStr, choColor, jungColor, jungColor, jongColor);
    }

    private Color GetColor(float score)
    {
        if (score >= 90) return Color.green;
        if (score >= 60) return Color.yellow;
        return Color.red;
    }

    private IEnumerator ClearFeedbackRoutine()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        if (feedbackTitleText != null) feedbackTitleText.text = "";
        foreach (Transform child in wordContainer) Destroy(child.gameObject);
    }

    public void ShowFeedbackMessage(string message) { if (feedbackTitleText != null) feedbackTitleText.text = message; }
    private void UpdateStatusText(string message) { if (statusText != null) statusText.text = message; }

    private void HandleGameStateChanged(GameState state)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (state == GameState.GameOver && gameOverPanel != null) gameOverPanel.SetActive(true);
        else if (state == GameState.LevelUp && levelUpPanel != null) levelUpPanel.SetActive(true);
    }
}