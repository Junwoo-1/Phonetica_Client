using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class TransitionController : MonoBehaviour
{
    public static TransitionController Instance { get; private set; }

    [Header("트랜지션 대상 UI 요소")]
    public RectTransform circleUI;

    [Header("쪼개진 제목 글자들 (이동 연출용)")]
    public HorizontalLayoutGroup titleLayoutGroup; // 정렬을 풀어주기 위한 컴포넌트
    public RectTransform[] leftTitleLetters;       // '포', '네' (왼쪽으로 갈 글자들)
    public RectTransform[] rightTitleLetters;      // '티', '카' (오른쪽으로 갈 글자들)

    [Header("움직일 버튼들")]
    public RectTransform settingButton;
    public RectTransform startButton;
    public RectTransform quitButton;

    [Header("연출 설정")]
    public float duration = 1.5f;
    public float targetScale = 50f;
    public float moveDistance = 800f;       // 버튼들이 날아갈 거리
    public float titleMoveDistance = 500f;  // 제목 글자들이 양옆으로 날아갈 거리

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartTransition()
    {
        gameObject.SetActive(true);

        // ⭐️ 중요: 애니메이션이 시작되기 직전에 LayoutGroup을 꺼야 글자들이 코드를 따라 움직일 수 있습니다!
        if (titleLayoutGroup != null) titleLayoutGroup.enabled = false;

        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        float elapsedTime = 0f;

        // 1. 원, 버튼 초기 위치 저장
        Vector3 circleStartScale = circleUI.localScale;
        Vector3 circleEndScale = new Vector3(targetScale, targetScale, 1f);

        Vector2 setBtnStart = settingButton.anchoredPosition;
        Vector2 startBtnStart = startButton.anchoredPosition;
        Vector2 quitBtnStart = quitButton.anchoredPosition;

        // 2. 제목 글자들의 초기 위치 저장
        Vector2[] leftStarts = new Vector2[leftTitleLetters.Length];
        for (int i = 0; i < leftTitleLetters.Length; i++) leftStarts[i] = leftTitleLetters[i].anchoredPosition;

        Vector2[] rightStarts = new Vector2[rightTitleLetters.Length];
        for (int i = 0; i < rightTitleLetters.Length; i++) rightStarts[i] = rightTitleLetters[i].anchoredPosition;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float easeIn = t * t; // 점점 빠르게 가속

            // 원 스케일 업 & 전체 투명도 조절
            circleUI.localScale = Vector3.Lerp(circleStartScale, circleEndScale, easeIn);

            // 버튼 밀어내기
            if (settingButton != null) settingButton.anchoredPosition = Vector2.Lerp(setBtnStart, setBtnStart + new Vector2(-moveDistance, 0), easeIn);
            if (startButton != null) startButton.anchoredPosition = Vector2.Lerp(startBtnStart, startBtnStart + new Vector2(0, -moveDistance), easeIn);
            if (quitButton != null) quitButton.anchoredPosition = Vector2.Lerp(quitBtnStart, quitBtnStart + new Vector2(moveDistance, 0), easeIn);

            // 1. 왼쪽 그룹 가르기 ('포', '네')
            // leftTitleLetters의 길이가 2일 때:
            // 인덱스 0 ('포') -> multiplier = 2 -> 2배의 거리(속도)로 날아감
            // 인덱스 1 ('네') -> multiplier = 1 -> 1배의 거리(속도)로 날아감
            for (int i = 0; i < leftTitleLetters.Length; i++)
            {
                if (leftTitleLetters[i] != null)
                {
                    float distanceMultiplier = leftTitleLetters.Length - i;
                    float actualDistance = titleMoveDistance * distanceMultiplier;

                    leftTitleLetters[i].anchoredPosition = Vector2.Lerp(
                        leftStarts[i],
                        leftStarts[i] + new Vector2(-actualDistance, 0),
                        easeIn
                    );
                }
            }

            // 2. 오른쪽 그룹 가르기 ('티', '카')
            // rightTitleLetters의 길이가 2일 때:
            // 인덱스 0 ('티') -> multiplier = 1 -> 1배의 거리(속도)로 날아감
            // 인덱스 1 ('카') -> multiplier = 2 -> 2배의 거리(속도)로 날아감
            for (int i = 0; i < rightTitleLetters.Length; i++)
            {
                if (rightTitleLetters[i] != null)
                {
                    float distanceMultiplier = i + 1;
                    float actualDistance = titleMoveDistance * distanceMultiplier;

                    rightTitleLetters[i].anchoredPosition = Vector2.Lerp(
                        rightStarts[i],
                        rightStarts[i] + new Vector2(actualDistance, 0),
                        easeIn
                    );
                }
            }

            yield return null;
        }

        UIManager.Instance.SwitchBasePanel(UIManager.Instance.selectPanel);
        GameManager.Instance.ChangeState(GameState.CategorySelect);
    }
}