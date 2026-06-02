using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 패널들")]
    public GameObject gameOverPanel;
    public GameObject levelUpPanel;

    [Header("음성 인식 UI")]
    public TextMeshProUGUI statusText;

    [Header("조립식 피드백 설정")]
    public Transform wordContainer;    // 정답 단어 (위쪽)
    public Transform heardContainer;   // 유저 실제 발음 단어 (아래쪽)
    public GameObject syllablePrefab;
    public TextMeshProUGUI feedbackTitleText;
    public float feedbackDisplayTime = 2.0f;

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

    // 매개변수에 heardJamos 리스트를 추가로 받습니다.
    public void ShowSyllableFeedback(string title, List<JamoScoreInfo> detailedJamos, List<JamoToken> heardJamos)
    {
        if (wordContainer == null || syllablePrefab == null) return;

        // 1. 기존 UI 초기화
        foreach (Transform child in wordContainer) Destroy(child.gameObject);
        if (heardContainer != null)
            foreach (Transform child in heardContainer) Destroy(child.gameObject);

        if (feedbackTitleText != null) feedbackTitleText.text = title;

        // 2. 정답 단어(detailed_jamos) 렌더링 - 기존과 동일
        if (detailedJamos != null)
        {
            var groupedBySyllable = detailedJamos.GroupBy(j => j.syl).OrderBy(g => g.Key);
            foreach (var group in groupedBySyllable)
            {
                GameObject syllableObj = Instantiate(syllablePrefab, wordContainer);
                var jamosInSyllable = group.ToList();
                var onset = jamosInSyllable.FirstOrDefault(j => j.pos == "onset");
                var nucleus = jamosInSyllable.FirstOrDefault(j => j.pos == "nucleus");
                var coda = jamosInSyllable.FirstOrDefault(j => j.pos == "coda");
                SetupSyllable(syllableObj, onset, nucleus, coda);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordContainer.GetComponent<RectTransform>());
        }

        // 3. 실제 발음 단어(heard_jamos) 렌더링
        if (heardJamos != null && heardContainer != null)
        {
            var groupedHeard = heardJamos.GroupBy(j => j.syl).OrderBy(g => g.Key);
            foreach (var group in groupedHeard)
            {
                GameObject syllableObj = Instantiate(syllablePrefab, heardContainer);
                var jamosInSyllable = group.ToList();

                // heard_jamos는 점수가 없는 JamoToken 형태입니다.
                var onset = jamosInSyllable.FirstOrDefault(j => j.pos == "onset");
                var nucleus = jamosInSyllable.FirstOrDefault(j => j.pos == "nucleus");
                var coda = jamosInSyllable.FirstOrDefault(j => j.pos == "coda");

                SetupHeardSyllable(syllableObj, onset, nucleus, coda);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(heardContainer.GetComponent<RectTransform>());
        }

        Canvas.ForceUpdateCanvases();
        StopCoroutine("ClearFeedbackRoutine");
        StartCoroutine("ClearFeedbackRoutine");
    }

    // 기존의 정답 세팅 함수 (색상 로직 포함)
    private void SetupSyllable(GameObject block, JamoScoreInfo cho, JamoScoreInfo jung, JamoScoreInfo jong)
    {
        SyllableUI syllableUI = block.GetComponent<SyllableUI>();
        if (syllableUI == null) return;

        string choStr = cho != null ? cho.@char : "";
        string jongStr = jong != null ? jong.@char : "";
        string jungStr1 = "";
        string jungStr2 = "";

        Color choColor = cho != null ? GetColor(cho.score) : Color.white;
        Color jungColor = jung != null ? GetColor(jung.score) : Color.white;
        Color jongColor = jong != null ? GetColor(jong.score) : Color.white;

        if (jung != null)
        {
            string[] splitVowels = JamoSplitter.Split(jung.@char);
            jungStr1 = splitVowels[0];
            if (splitVowels.Length > 1) jungStr2 = splitVowels[1];
        }

        syllableUI.SetSyllable(choStr, jungStr1, jungStr2, jongStr, choColor, jungColor, jungColor, jongColor);
    }

    // 실제 발음 세팅 함수 (점수 없이 흰색/회색으로 렌더링)
    private void SetupHeardSyllable(GameObject block, JamoToken cho, JamoToken jung, JamoToken jong)
    {
        SyllableUI syllableUI = block.GetComponent<SyllableUI>();
        if (syllableUI == null) return;

        string choStr = cho != null ? cho.@char : "";
        string jongStr = jong != null ? jong.@char : "";
        string jungStr1 = "";
        string jungStr2 = "";

        if (jung != null)
        {
            string[] splitVowels = JamoSplitter.Split(jung.@char);
            jungStr1 = splitVowels[0];
            if (splitVowels.Length > 1) jungStr2 = splitVowels[1];
        }

        // 실제 발음은 비교군이므로 담백하게 흰색(또는 밝은 회색)으로 고정합니다.
        Color defaultColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        syllableUI.SetSyllable(choStr, jungStr1, jungStr2, jongStr, defaultColor, defaultColor, defaultColor, defaultColor);
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

        // ⭐️ 실제 발음 컨테이너도 함께 비워줍니다.
        if (heardContainer != null)
            foreach (Transform child in heardContainer) Destroy(child.gameObject);
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