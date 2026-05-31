using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MainPlayWindowController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainUIElements; // 숨길 메인화면 요소 그룹 (Deco, PlayBtn, QuitBtn, SettingsBtn을 묶은 부모나 MainPanel 연결)
    public RectTransform playContents; // 새로 만든 PlayContents (RectTransform 연결)
    public Button backButton;          // PlayWindow 우측 하단의 Back 버튼

    private Button thisButton;
    private Vector2 contentReadyPos;   // 창이 열렸을 때 위치 (-550, -380)
    private Vector2 contentExitPos;    // 창이 닫힐 때 숨겨질 위치 (예: 화면 아래쪽 외곽)

    void Start()
    {
        thisButton = GetComponent<Button>();

        contentReadyPos = new Vector2(0f, 0f);
        // 닫혀있을 때는 화면 아래 바깥(-1500)에 대기
        contentExitPos = new Vector2(550f, -1500f);

        if (playContents != null)
        {
            playContents.anchoredPosition = contentExitPos;
            playContents.gameObject.SetActive(false);
        }

        // 버튼 리스너 연결
        thisButton.onClick.AddListener(OpenPlayWindow);
        if (backButton != null)
            backButton.onClick.AddListener(ClosePlayWindow);
    }

    // [열기] 플레이 모드 선택창 등장
    public void OpenPlayWindow()
    {
        thisButton.interactable = false;

        // 1. 레이어 꼬임 방지를 위해 메인 글자/오브젝트들 숨기기
        if (mainUIElements != null)
            mainUIElements.SetActive(false);

        // 2. 플레이 창 활성화 후 아래에서 위로 '샥-' 올라오는 연출
        if (playContents != null)
        {
            playContents.gameObject.SetActive(true);
            playContents.DOAnchorPos(contentReadyPos, 0.4f).SetEase(Ease.OutCubic);
        }
    }

    // [닫기] 다시 메인 화면으로 복귀 (역순)
    public void ClosePlayWindow()
    {
        if (playContents != null)
        {
            // 1. 플레이 창이 아래로 '샥-' 내려가서 사라짐
            playContents.DOAnchorPos(contentExitPos, 0.35f).SetEase(Ease.InCubic)
            .OnComplete(() => {
                playContents.gameObject.SetActive(false);

                // 2. 창이 완전히 꺼진 후 메인 화면 요소들 복구
                if (mainUIElements != null)
                    mainUIElements.SetActive(true);

                // 3. 플레이 버튼 클릭 기능 복구
                thisButton.interactable = true;
            });
        }
    }
}