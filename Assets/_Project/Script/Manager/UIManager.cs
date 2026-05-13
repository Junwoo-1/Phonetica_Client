using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI 패널들")]
    public GameObject gameOverPanel;
    public GameObject levelUpPanel;
    // public GameObject pausePanel; // 나중에 추가할 수 있습니다.

    [Header("음성 인식 UI")]
    public TextMeshProUGUI statusText;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        
        // 네트워크 매니저의 상태 업데이트 방송을 구독합니다.
        if (PronunciationClient.Instance != null)
        {
            PronunciationClient.Instance.OnStatusUpdated += UpdateStatusText;
        }
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        
        if (PronunciationClient.Instance != null)
        {
            PronunciationClient.Instance.OnStatusUpdated -= UpdateStatusText;
        }
    }

    // 방송이 올 때마다 텍스트를 바꿉니다.
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    // 방송이 들어올 때마다 실행될 함수
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