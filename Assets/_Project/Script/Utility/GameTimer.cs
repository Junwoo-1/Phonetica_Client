using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("시간 설정 (초 단위)")]
    public float maxTime = 180f; // 기본 3분 (180초) 
    private float _currentTime = 0f;

    [Header("UI 연결")]
    public TextMeshProUGUI timerText; // 화면에 시간을 띄울 텍스트

    void Update()
    {
        // 게임 중(Playing)일 때만 시간이 흘러갑니다. (레벨업 창에선 시간 정지!)
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        _currentTime += Time.deltaTime; // 여기서는 현실 시간이 아닌 '게임 시간'을 씁니다.

        UpdateTimerUI();

        // 목표 시간에 도달하면? -> 버티기 성공!
        if (_currentTime >= maxTime)
        {
            GameClear();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            // 남은 시간으로 보여줄지, 흘러간 시간으로 보여줄지 선택하세요! (현재는 남은 시간 카운트다운)
            float timeLeft = Mathf.Max(maxTime - _currentTime, 0); 
            
            int minutes = Mathf.FloorToInt(timeLeft / 60F);
            int seconds = Mathf.FloorToInt(timeLeft - minutes * 60);
            
            // "03:00" 형식으로 예쁘게 포맷팅
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void GameClear()
    {
        Debug.Log("[GameTimer] 제한 시간 도달! 게임 클리어 (버티기 성공)!");

        ResultUI.IsClearData = true;

        GameManager.Instance.ChangeState(GameState.GameOver); 
    }
}