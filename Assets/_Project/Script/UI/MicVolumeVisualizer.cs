using UnityEngine;
using UnityEngine.UI;

public class MicVolumeVisualizer : MonoBehaviour
{
    [Header("UI 연결")]
    public Image fillWaveImageL;
    public Image fillWaveImageR;

    [Header("비주얼라이저 설정")]
    public float sensitivity = 50f;
    public float smoothSpeed = 15f;

    private float _currentDisplayedVolume = 0f;

    // 🚨 마이크를 이중으로 켜던 Start() 함수는 완전히 삭제했습니다!

    void Update()
    {
        // 1. 녹음기가 켜져 있는지 확인합니다.
        if (VoiceRecorder.Instance == null) return;

        // 2. 메인 녹음기에서 실시간 볼륨 수치를 훔쳐옵니다! (핵심)
        float rawVolume = VoiceRecorder.Instance.CurrentVolume;

        // 3. 민감도를 곱해주고 0~1 사이로 자릅니다.
        float targetFillAmount = Mathf.Clamp01(rawVolume * sensitivity);

        // 4. 파형이 부드럽게 오르내리도록 보정합니다.
        _currentDisplayedVolume = Mathf.Lerp(_currentDisplayedVolume, targetFillAmount, Time.unscaledDeltaTime * smoothSpeed);

        // 5. 파형 UI에 적용합니다!
        if (fillWaveImageL != null && fillWaveImageR != null)
        {
            fillWaveImageL.fillAmount = _currentDisplayedVolume;
            fillWaveImageR.fillAmount = _currentDisplayedVolume;
        }
    }
}