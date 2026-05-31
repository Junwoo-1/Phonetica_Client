using UnityEngine;
using UnityEngine.UI;

public class QuitGameController : MonoBehaviour
{
    void Start()
    {
        // 스크립트가 붙은 버튼 컴포넌트를 자동으로 가져와서 클릭 이벤트를 연결합니다.
        Button quitButton = GetComponent<Button>();

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료 버튼 클릭됨!");

        // 1. 유니티 에디터에서 플레이 중일 때 끄기
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 2. 실제 빌드된 PC 게임(전체화면/창모드)에서 실행 중일 때 끄기
        Application.Quit();
#endif
    }
}