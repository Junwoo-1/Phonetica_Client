using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class DynamicSettingsTransition : MonoBehaviour
{
    [Header("--- UI References ---")]
    [Tooltip("설정창 켜질 때 숨길 다른 메인 UI들 (Play, Quit, Deco 등)")]
    [SerializeField] private GameObject mainUIElements;

    [Tooltip("MainPanel 하위로 빼둔 설정창 패널")]
    [SerializeField] private RectTransform settingsWindow;

    [SerializeField] private Button doneButton;

    [Header("--- Layout Coordinates ---")]
    [Tooltip("설정창 패널이 화면 중앙에 들어왔을 때의 목적지 좌표")]
    [SerializeField] private Vector2 panelOpenPosition = new Vector2(0f, 0f);

    [Tooltip("설정창이 평소에 대기할 왼쪽 화면 밖 좌표")]
    [SerializeField] private Vector2 panelExitPosition = new Vector2(-1500f, 0f);

    [Tooltip("Settings 텍스트가 위로 올라갈 상대 좌표")]
    [SerializeField] private Vector2 titleOpenPosition = new Vector2(0f, 870f);

    // 컴포넌트 캐싱
    private TMP_Text btnText;
    private RectTransform textRect;
    private Button thisButton;

    // 텍스트 원래 자리 기억
    private Vector2 originTextPos;

    void Start()
    {
        thisButton = GetComponent<Button>();
        btnText = GetComponentInChildren<TMP_Text>();
        textRect = btnText.GetComponent<RectTransform>();

        // 1. Settings 텍스트의 초기 위치 기억
        originTextPos = textRect.anchoredPosition;

        // 2. 설정창 패널 초기화 (화면 왼쪽 밖으로 치우고 꺼두기)
        if (settingsWindow != null)
        {
            settingsWindow.anchoredPosition = panelExitPosition;
            settingsWindow.gameObject.SetActive(false);
        }

        // 3. 버튼 클릭 리스너 연결
        thisButton.onClick.AddListener(OpenSettingsPanel);
        if (doneButton != null)
            doneButton.onClick.AddListener(CloseSettingsPanel);
    }

    /// <summary>
    /// [열기] Settings 텍스트는 위로 띄우고, 패널은 옆에서 끌고 옵니다.
    /// </summary>
    public void OpenSettingsPanel()
    {
        thisButton.interactable = false;

        // 1. 방해되는 다른 메인 UI 끄기 (PlayBtn, QuitBtn 등)
        if (mainUIElements != null)
            mainUIElements.SetActive(false);

        // 2. Settings 텍스트 위로 '샥' 올리기
        textRect.DOAnchorPos(titleOpenPosition, 0.4f).SetEase(Ease.OutCubic);

        // 3. 밖에서 대기하던 설정창 패널이 왼쪽에서 '샥' 들어오기
        if (settingsWindow != null)
        {
            settingsWindow.gameObject.SetActive(true);
            settingsWindow.anchoredPosition = panelExitPosition; // 출발지 확정
            settingsWindow.DOAnchorPos(panelOpenPosition, 0.4f).SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// [닫기] 패널을 옆으로 치우고, Settings 텍스트를 제자리로 내립니다.
    /// </summary>
    public void CloseSettingsPanel()
    {
        if (settingsWindow == null) return;

        // 1. 설정창 패널이 왼쪽 화면 밖으로 '샥' 빠짐
        settingsWindow.DOAnchorPos(panelExitPosition, 0.35f).SetEase(Ease.InCubic)
        .OnComplete(() =>
        {
            settingsWindow.gameObject.SetActive(false);

            // 2. 메인 UI 다시 켜기
            if (mainUIElements != null)
                mainUIElements.SetActive(true);

            // 3. Settings 텍스트 원래 자리로 '샥' 내려오기
            textRect.DOAnchorPos(originTextPos, 0.4f).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                // 모든 연출 완료 후 클릭 복구
                thisButton.interactable = true;
            });
        });
    }
}