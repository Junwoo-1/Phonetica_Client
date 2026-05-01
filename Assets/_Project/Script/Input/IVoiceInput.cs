using System;

// 단어, 발음 정확도, 억양 점수를 담는 데이터 바구니
public struct VoiceData
{
    public string word;
    public float accuracy;
    public float pitchScore;
}

public interface IVoiceInput
{
    // 입력이 들어왔을 때 발생할 이벤트 (옵저버 패턴)
    event Action<VoiceData> OnWordSpoken; 
}