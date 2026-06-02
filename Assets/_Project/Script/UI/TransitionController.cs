using System.Collections;
using UnityEngine;

public class TransitionController : MonoBehaviour
{
    public static TransitionController Instance { get; private set; }

    [Header("트랜지션 UI 요소")]
    public RectTransform circleUI;      // 화면 중앙의 얇은 원
    public CanvasGroup textCanvasGroup; // PHONETICA, 버튼 등 글자들이 묶인 그룹
    
    [Header("연출 설정")]
    public float duration = 1.5f;       // 애니메이션 진행 시간 (1.5초)
    public float targetScale = 50f;     // 원이 얼마나 커질지 (화면을 덮을 만큼 큰 숫자)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // GameManager 등에서 상태가 Transition으로 바뀔 때 이 함수를 호출합니다.
    public void StartTransition()
    {
        gameObject.SetActive(true);
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        float elapsedTime = 0f;
        Vector3 initialScale = circleUI.localScale;
        Vector3 finalScale = new Vector3(targetScale, targetScale, 1f);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 0 ~ 1 사이의 진행도 (t)
            float t = elapsedTime / duration;
            
            // 1. 원 크기 키우기 (t * t를 곱하면 처음엔 천천히, 뒤에는 확 커지는 가속 효과가 납니다)
            float easeIn = t * t;
            circleUI.localScale = Vector3.Lerp(initialScale, finalScale, easeIn);

            // 2. 텍스트 페이드 아웃 (투명도 1 -> 0으로 스르륵 사라짐)
            if (textCanvasGroup != null)
            {
                textCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            yield return null; // 다음 프레임까지 대기
        }

        // 3. 연출이 완전히 끝나면, 게임 상태를 Playing으로 바꿔서 적 스폰을 시작합니다!
        GameManager.Instance.ChangeState(GameState.Playing);
        
        // (선택) 더 이상 안 쓰는 UI는 꺼줍니다.
        if (textCanvasGroup != null) textCanvasGroup.gameObject.SetActive(false);
    }
}