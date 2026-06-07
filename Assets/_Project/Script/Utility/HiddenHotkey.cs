using UnityEngine;
using UnityEngine.SceneManagement; // 씬 리로드를 위해 필수

public class HiddenHotkey : MonoBehaviour
{
    [Header("히든 커맨드 설정")]
    public KeyCode reloadKey = KeyCode.Escape; // 기본값을 ESC로 설정합니다.

    // 마우스 클릭(OnClick)과 달리, 키보드 입력은 매 프레임(Update)마다 눌렸는지 검사해야 합니다.
    private void Update()
    {
        // 지정한 키(ESC)가 찰나의 순간에 '눌렸을' 때 작동합니다.
        if (Input.GetKeyDown(reloadKey))
        {
            Debug.Log("[HiddenHotkey] ESC 히든 커맨드 감지! 게임을 강제로 초기화합니다.");

            // 1. Static(전역) 바구니 강제 비우기 (UIManager에서 하던 청소 작업)
            LevelUpUI.UnlockedCategories.Clear();
            ResultUI.IsClearData = false;

            // 2. 레벨업 화면 등에서 시간이 멈춰있을 수 있으니 무조건 원상복구
            Time.timeScale = 1f;

            // 3. 현재 씬을 자비 없이 처음부터 다시 불러옵니다.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}