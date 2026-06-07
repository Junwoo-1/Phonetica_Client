using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 이 스크립트를 넣으면 유니티가 알아서 Button 컴포넌트도 같이 달아줍니다!
[RequireComponent(typeof(Button))]
public class ClickToCommand : MonoBehaviour
{
    [Header("고정 명령어 (비워두면 텍스트를 자동 인식합니다)")]
    public string manualCommand = "";

    private void Start()
    {
        // 버튼 클릭 이벤트에 아래 OnClick 함수를 연결합니다.
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        string finalCommand = manualCommand;

        // 인스펙터에 명령어를 안 적어뒀다면? 자기 자신의 글자를 실시간으로 읽어옵니다.
        // (레벨업이나 카테고리처럼 글자가 매번 바뀌는 UI를 위한 완벽한 처리입니다!)
        if (string.IsNullOrEmpty(finalCommand))
        {
            TextMeshProUGUI textMesh = GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
            {
                finalCommand = textMesh.text.Trim();
            }
        }

        Debug.Log($"[Click] 마우스 클릭 감지됨: {finalCommand}");

        // UIManager의 음성 인식 처리기에게 단어를 그대로 토스합니다!
        if (UIManager.Instance != null && !string.IsNullOrEmpty(finalCommand))
        {
            UIManager.Instance.HandleVoiceCommand(finalCommand);
        }
    }
}