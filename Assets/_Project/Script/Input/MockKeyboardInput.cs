using System;
using UnityEngine;

public class MockKeyboardInput : MonoBehaviour, IVoiceInput
{
    public event Action<VoiceData> OnWordSpoken;

    // 인스펙터에서 테스트할 단어를 직접 적을 수 있게 합니다.
    [Header("테스트용 단어 입력 (엔터키로 발사)")]
    public string testWord = "사과";

    void Update()
    {
        // 엔터키를 누르면 음성 인식이 완료된 것처럼 꾸밉니다.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            VoiceData dummyData = new VoiceData
            {
                word = testWord,
                accuracy = UnityEngine.Random.Range(0.7f, 1.0f), // 70~100점 사이 랜덤
                pitchScore = UnityEngine.Random.Range(0.5f, 1.0f)
            };

            Debug.Log($"[MockInput] 인식된 단어: {dummyData.word}");
            
            // 구독하고 있는 Player에게 데이터를 쏴줍니다.
            OnWordSpoken?.Invoke(dummyData);
        }
    }
}