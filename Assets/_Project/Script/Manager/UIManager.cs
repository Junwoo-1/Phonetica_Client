using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 패널들 (등록용)")]
    public GameObject mainmenuPanel;
    public GameObject settingPanel;
    public GameObject gamePanel;
    public GameObject selectPanel;
    public GameObject levelUpPanel;
    public GameObject gameoverPanel;

    // ⭐️ [NEW] 패널들을 스택으로 관리합니다.
    private Stack<GameObject> _uiStack = new Stack<GameObject>();

    [Header("음성 인식 커맨드")]
    [SerializeField] private string startCommand = "시작";
    [SerializeField] private string settingCommand = "설정";
    [SerializeField] private string quitCommand = "종료";
    [SerializeField] private string backCommand = "뒤로";

    [Header("조립식 피드백 설정")]
    public Transform wordContainer;
    public Transform heardContainer;
    public GameObject syllablePrefab;
    public float feedbackDisplayTime = 2.0f;

    public GameObject feedbackBackground;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        TurnOffAllPanels();
        PushPanel(mainmenuPanel);

        // [NEW] 서버 객체가 안전하게 생성된 후인 Start()에서 구독합니다!
        if (PronunciationClient.Instance != null)
            PronunciationClient.Instance.OnScoreReceived += HandleVoiceCommand;

        // [NEW] 게임 시작 시 현재 상태를 MainMenu로 확실하게 고정합니다.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MainMenu)
            GameManager.Instance.ChangeState(GameState.MainMenu);
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;

    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;

        // 추가: 구독 해제
        if (PronunciationClient.Instance != null)
            PronunciationClient.Instance.OnScoreReceived -= HandleVoiceCommand;
    }

    #region 스택(Stack) 기반 UI 관리 로직

    // 1. 새 패널을 화면에 띄우고 스택에 쌓습니다 (예: 설정창 열기)
    public void PushPanel(GameObject panelToPush)
    {
        if (panelToPush == null) return;
        panelToPush.SetActive(true);
        _uiStack.Push(panelToPush);
    }

    // 2. 가장 위에 있는 패널을 닫습니다 (예: 설정창 닫기, 뒤로가기)
    public void PopPanel()
    {
        if (_uiStack.Count <= 1) return; // 메인 메뉴(바닥)는 팝업처럼 닫히지 않게 방어

        GameObject topPanel = _uiStack.Pop();
        topPanel.SetActive(false);
    }

    // 3. 모든 창을 다 닫고 메인 메뉴로 돌아갑니다 (게임 오버 후 복귀 시 사용)
    public void ClearToMainMenu()
    {
        while (_uiStack.Count > 1)
        {
            GameObject panel = _uiStack.Pop();
            panel.SetActive(false);
        }

        // 만약 메인 메뉴마저 꺼져있다면 다시 켜줍니다.
        if (!mainmenuPanel.activeSelf) mainmenuPanel.SetActive(true);
    }

    // 4. (특수) 트랜지션 후 메인 메뉴를 숨기고 다른 화면으로 강제 전환할 때 사용
    public void SwitchBasePanel(GameObject newBasePanel)
    {
        TurnOffAllPanels();
        _uiStack.Clear();
        PushPanel(newBasePanel);
    }

    private void TurnOffAllPanels()
    {
        mainmenuPanel.SetActive(false);
        settingPanel.SetActive(false);
        gamePanel.SetActive(false);
        selectPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        gameoverPanel.SetActive(false);
    }

    #endregion

    private void HandleGameStateChanged(GameState state)
    {
        // GameManager의 상태 변화에 따라 스택을 조작합니다.
        switch (state)
        {
            case GameState.LevelUp:
                PushPanel(levelUpPanel); // 게임 패널 위에 덮어씌움
                break;
            case GameState.GameOver:
                PushPanel(gameoverPanel); // 게임 패널 위에 덮어씌움
                break;
            case GameState.Playing:
                // 레벨업이 끝났거나 일시정지가 풀렸을 때 윗 패널을 제거
                if (_uiStack.Peek() == levelUpPanel) PopPanel();
                break;
            case GameState.MainMenu:
                ClearToMainMenu();
                break;
        }
    }

    // 매개변수에 heardJamos 리스트를 추가로 받습니다.
    public void ShowSyllableFeedback(string title, List<JamoScoreInfo> detailedJamos, List<JamoToken> heardJamos)
    {
        if (wordContainer == null || syllablePrefab == null) return;

        if (feedbackBackground != null) feedbackBackground.SetActive(true);

        // 1. 기존 UI 초기화
        foreach (Transform child in wordContainer) Destroy(child.gameObject);
        if (heardContainer != null)
            foreach (Transform child in heardContainer) Destroy(child.gameObject);

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
        Color defaultColor = new Color(1f, 1f, 1f, 1f);
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
        foreach (Transform child in wordContainer) Destroy(child.gameObject);

        // ⭐️ 실제 발음 컨테이너도 함께 비워줍니다.
        if (heardContainer != null)
            foreach (Transform child in heardContainer) Destroy(child.gameObject);

        if (feedbackBackground != null) feedbackBackground.SetActive(false);
    }

    public List<string> GetActiveUICommands()
    {
        List<string> commands = new List<string>();
        if (GameManager.Instance == null) return commands;

        GameState currentState = GameManager.Instance.CurrentState;

        if (currentState == GameState.MainMenu)
        {
            // 현재 스택의 최상단 패널이 무엇인지 확인합니다.
            GameObject topPanel = _uiStack.Count > 0 ? _uiStack.Peek() : null;

            if (topPanel == settingPanel)
            {
                // 설정창이 켜져 있을 때는 "뒤로"만 장전합니다.
                commands.Add(backCommand);

                if (SettingUI.Instance != null)
                {
                    commands.AddRange(SettingUI.Instance.GetCommands());
                }
            }
            else if (topPanel == mainmenuPanel)
            {
                // 메인 메뉴일 때는 "시작", "설정", "종료" 세 발을 장전합니다.
                commands.Add(startCommand);
                commands.Add(settingCommand);
                commands.Add(quitCommand);
            }
        }
        else if (currentState == GameState.CategorySelect)
        {
            // 현재 화면에 떠 있는 3개의 카테고리 이름을 후보군으로 장전합니다.
            if (CategorySelectionUI.Instance != null)
            {
                commands.AddRange(CategorySelectionUI.Instance.GetCurrentCategoryNames());
            }
        }
        else if (currentState == GameState.LevelUp)
        {
            if (LevelUpUI.Instance != null)
            {
                // 레벨업 화면에 뜬 카테고리 이름들을 마이크 장전 후보에 추가합니다.
                commands.AddRange(LevelUpUI.Instance.GetCurrentUpgrades());
            }
        }
        return commands;
    }
    private void HandleVoiceCommand(ScorePayload payload)
    {
        // Trim()을 사용해 양옆의 보이지 않는 공백을 완벽히 제거합니다.
        string word = payload.recognized_word.Trim();
        if (string.IsNullOrEmpty(word)) return;

        GameState currentState = GameManager.Instance.CurrentState;
        Debug.Log($"[UIManager] 명령 수신: {word} / 현재 게임 상태: {currentState}");

        // 1. 메인 메뉴 상태일 때의 제어
        if (currentState == GameState.MainMenu)
        {
            GameObject topPanel = _uiStack.Count > 0 ? _uiStack.Peek() : null;

            if (topPanel == settingPanel)
            {
                // 설정창이 켜져 있을 때
                if (word == backCommand)
                {
                    PopPanel(); // "뒤로" 말하면 창 닫기
                }
                else if (SettingUI.Instance != null)
                {
                    // "볼륨 크게" 등을 말하면 SettingUI에게 처리를 토스합니다.
                    SettingUI.Instance.HandleCommand(word);
                }
            }
            else // mainmenuPanel이 최상단일 때
            {
                if (word == startCommand)
                {
                    if (TransitionController.Instance != null) TransitionController.Instance.StartTransition();
                }
                else if (word == settingCommand) PushPanel(settingPanel);
                else if (word == quitCommand)
                {
                    Debug.Log("[UIManager] 게임 종료 명령이 실행되었습니다!");

                    // ⭐️ 에디터에서 실행 중일 때는 Play 모드를 정지시킵니다.
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
        // ⭐️ 실제 빌드된 게임일 때는 정상적으로 앱을 종료합니다.
        Application.Quit();
#endif
                }
            }
        }
        // 2. 카테고리 선택 상태 (여기가 분리되어 있어야 합니다!)
        else if (currentState == GameState.CategorySelect)
        {
            if (CategorySelectionUI.Instance != null)
            {
                CategorySelectionUI.Instance.SelectCategoryByName(word);
            }
            else
            {
                Debug.LogError("[UIManager] CategorySelectionUI 인스턴스를 찾을 수 없습니다!");
            }
        }

        else if (currentState == GameState.LevelUp)
        {
            if (LevelUpUI.Instance != null)
            {
                LevelUpUI.Instance.SelectUpgrade(word); // 여기서 LevelUpUI로 단어를 넘겨줍니다!
            }
        }
    }

    public void HideFeedbackImmediate()
    {
        if (wordContainer != null) foreach (Transform child in wordContainer) Destroy(child.gameObject);
        if (heardContainer != null) foreach (Transform child in heardContainer) Destroy(child.gameObject);
        if (feedbackBackground != null) feedbackBackground.SetActive(false);
    }
}