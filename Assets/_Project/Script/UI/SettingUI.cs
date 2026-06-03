using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SettingUI : MonoBehaviour
{
    public static SettingUI Instance { get; private set; }

    [Header("UI 연결")]
    public Slider volumeSlider; // 볼륨을 보여줄 슬라이더 UI

    // 음성 명령어와 그에 맞는 볼륨 수치(0~1)를 매칭해둔 딕셔너리(사전)입니다.
    private Dictionary<string, float> _volumeCommands = new Dictionary<string, float>
    {
        { "볼륨최소", 0f },
        { "볼륨작게", 0.25f },
        { "볼륨중간", 0.5f },
        { "볼륨크게", 0.75f },
        { "볼륨최대", 1f }
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (volumeSlider != null)
        {
            // 유저가 혹시라도 마우스로 슬라이더를 만질 경우를 대비해 연동해 둡니다.
            volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);

            // 게임 시작 시 기본 볼륨을 '중간(0.5)'으로 세팅합니다.
            volumeSlider.value = 0.5f;
        }
    }

    // UIManager가 장전할 단어 목록을 넘겨주는 함수
    public List<string> GetCommands()
    {
        return _volumeCommands.Keys.ToList();
    }

    // 유저가 설정창에서 단어를 말했을 때 실행되는 함수
    public void HandleCommand(string command)
    {
        // 인식된 단어가 딕셔너리에 있다면?
        if (_volumeCommands.ContainsKey(command))
        {
            float targetVolume = _volumeCommands[command];

            // 1. 슬라이더 바를 움직입니다. 
            // (슬라이더의 value가 변하면 자동으로 아래의 OnSliderValueChanged 함수가 실행됩니다)
            if (volumeSlider != null)
            {
                volumeSlider.value = targetVolume;
            }

            Debug.Log($"[SettingUI] '{command}' 인식됨! 슬라이더를 {targetVolume}로 이동합니다.");
        }
    }

    // 슬라이더 값이 변할 때 실제 사운드 매니저의 소리를 조절합니다.
    private void OnSliderValueChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume(value);
        }
    }
}