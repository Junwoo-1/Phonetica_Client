using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI 패널들")]
    public GameObject gameOverPanel;
    public GameObject levelUpPanel;
    // public GameObject pausePanel; // 나중에 추가할 수 있습니다.

    private void OnEnable()
    {
        // 스크립트가 켜질 때 GameManager의 방송을 구독합니다.
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        // 메모리 누수를 막기 위해 스크립트가 꺼질 때 구독을 취소합니다.
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    // ⭐️ 방송이 들어올 때마다 실행될 함수
    private void HandleGameStateChanged(GameState state)
    {
        // 1. 일단 모든 특수 패널을 끕니다.
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);

        // 2. 현재 상태에 맞는 패널만 켭니다.
        if (state == GameState.GameOver && gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else if (state == GameState.LevelUp && levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
        }
    }
}