using UnityEngine;
using System; // Action 이벤트를 위해 필요

// ⭐️ 1. 우리 게임이 가질 수 있는 모든 '상태'를 정의합니다.
public enum GameState
{
    MainMenu,       // 메인 화면
    CategorySelect, // 카테고리 선택 화면
    Transition,     // 화면 전환 연출 중
    Playing,        // 실제 게임 플레이 중 (기존에 있다면 유지)
    LevelUp,        // 경험치를 꽉 채워서 업그레이드 창이 뜬 상태 (시간 정지)
    Paused,         // 유저가 ESC를 눌러 일시정지한 상태 (시간 정지)
    GameOver        // 체력이 0이 되어 게임이 끝난 상태 (시간 정지)
}

public class GameManager : MonoBehaviour
{
    [Header("게임 오브젝트 연결")]
    public GameObject playerObject;

    // 싱글톤 패턴: 어디서든 GameManager.Instance 로 접근할 수 있게 만듭니다.
    public static GameManager Instance { get; private set; }

    // 현재 게임의 상태를 보관하는 변수
    public GameState CurrentState { get; private set; }

    // 상태가 바뀔 때마다 다른 스크립트들에게 알려줄 '방송국(이벤트)'
    public static event Action<GameState> OnGameStateChanged;

    private void Awake()
    {

        Application.targetFrameRate = 60;
        // 싱글톤 초기화: 씬에 매니저가 오직 1개만 존재하도록 보장합니다.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 실수로 2개가 생기면 하나를 지웁니다.
        }
    }

    private void Start()
    {
        // 게임이 시작되면 자동으로 'MainMenu' 상태로 전환합니다.
        ChangeState(GameState.MainMenu);
    }

    // 3. 외부(Player 등)에서 상태를 바꾸고 싶을 때 호출하는 핵심 함수입니다.
    public void ChangeState(GameState newState)
    {
        // 이미 같은 상태라면 무시합니다.
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] 게임 상태 변경: {newState}");

        // 상태에 따른 '시간 제어(Time.timeScale)' 및 전용 로직 처리
        switch (newState)
        {
            case GameState.MainMenu:
            case GameState.CategorySelect:
                // 게임오버(0f) 상태에서 다시 메인 메뉴로 돌아올 때를 대비해 1f로 초기화합니다.
                Time.timeScale = 1f; 
                break;

            case GameState.Transition:
                Time.timeScale = 1f; // 코루틴 애니메이션(Time.deltaTime)이 정상 작동해야 하므로 1f 유지
                
                // 트랜지션 상태가 되면 연출 컨트롤러에게 애니메이션 시작 명령을 내립니다!
                if (TransitionController.Instance != null)
                {
                    TransitionController.Instance.StartTransition();
                }
                else
                {
                    Debug.LogWarning("[GameManager] 씬에 TransitionController가 없습니다! 바로 Playing으로 넘어갑니다.");
                    ChangeState(GameState.Playing); // 방어 로직: 컨트롤러가 없으면 진행이 막히지 않게 즉시 게임 시작
                }
                break;

            case GameState.Playing:
                Time.timeScale = 1f; // 정상 속도로 시간이 흐름
                if (playerObject != null) playerObject.SetActive(true);
                break;

            case GameState.LevelUp:
            case GameState.Paused:
            case GameState.GameOver:
                Time.timeScale = 0f; // 게임 내 모든 물리 연산과 Update(Time.deltaTime)를 멈춤!
                break;
        }

        // 상태가 바뀌었다고 이벤트를 구독한 모든 녀석들에게 방송을 쏩니다.
        // 예: UIManager는 이걸 듣고 게임오버 창을 띄우거나, Player는 움직임을 멈춥니다.
        OnGameStateChanged?.Invoke(newState);
    }
}