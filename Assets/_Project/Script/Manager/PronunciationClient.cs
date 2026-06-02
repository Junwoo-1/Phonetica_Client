using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ⭐️ [NEW] 개별 자모 점수 데이터 클래스
[Serializable]
public class JamoScoreInfo
{
    public string @char;
    public string pos;
    public float score;
    public int syl; // ⭐️ [NEW] 서버에서 보낸 글자 인덱스 받기
}

[Serializable]
public class JamoToken
{
    public string @char;
    public string pos;
    public int syl;
}

[Serializable]
public class SSEEnvelope
{
    public string @event;
    public ScorePayload data;
    public string ts;
    public string request_id;
}

[Serializable]
public class PositionAccuracy
{
    public int matched;
    public int total;
    public float accuracy;
}

[Serializable]
public class PerPosition
{
    public PositionAccuracy onset;
    public PositionAccuracy nucleus;
    public PositionAccuracy coda;
}

[Serializable]
public class ScorePayload
{
    public float overall_score;
    public string recognized_word;

    // ❌ 삭제됨: public string whisper_text;

    // ⭐️ 정답 기준 채점 결과 (기존과 동일, UI 조립용)
    public JamoScoreInfo[] detailed_jamos;

    // ⭐️ [NEW] 유저가 실제로 발음한 궤적 데이터 (매우 중요!)
    public JamoToken[] heard_jamos;

    // ⭐️ [NEW] 발음 에러율 데이터
    public float per;
    public float weighted_per;

    public JamoToken[] ref_jamo;
    public PerPosition per_position;
    public string[] problem_jamos;

    // ⭐️ [NEW] (선택) 서버 신뢰도 부족 여부
    public bool low_confidence;
}

public class PronunciationClient : MonoBehaviour
{
    public static PronunciationClient Instance { get; private set; }

    [Header("서버 설정")]
    public string serverUrl = "http://localhost:8000/pronounce";

    public event Action<ScorePayload> OnScoreReceived;
    public event Action<string> OnStatusUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ⭐️ [NEW] candidates 파라미터 추가
    public void SendAudioToServer(byte[] wavData, string candidates)
    {
        StartCoroutine(UploadAndReceiveSSE(wavData, candidates));
    }

    private IEnumerator UploadAndReceiveSSE(byte[] wavData, string candidates)
    {
        OnStatusUpdated?.Invoke("서버 연결 중...");

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "voice.wav", "audio/wav");

        // ⭐️ [NEW] 후보군 텍스트를 폼 데이터로 추가
        form.AddField("candidates", candidates);

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            request.SetRequestHeader("Accept", "text/event-stream");
            request.downloadHandler = new SSEDownloadHandler(ParseSingleEvent);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[네트워크 에러] {request.error}");
                OnStatusUpdated?.Invoke("통신 오류 발생!");
            }
        }
    }

    // 넘어온 텍스트 조각들을 이벤트 단위로 조립하고 해석하는 핵심 로직
    private void ProcessSSEChunk(string chunk, StringBuilder buffer)
    {
        buffer.Append(chunk);
        string currentBuffer = buffer.ToString();

        // SSE 이벤트는 보통 "\n\n"으로 끝납니다. 
        int delimiterIndex;
        while ((delimiterIndex = currentBuffer.IndexOf("\n\n")) >= 0)
        {
            // 완전한 이벤트 블록 하나를 뽑아냅니다.
            string eventBlock = currentBuffer.Substring(0, delimiterIndex);
            
            // 처리한 블록은 버퍼에서 날립니다.
            currentBuffer = currentBuffer.Substring(delimiterIndex + 2);
            buffer.Clear();
            buffer.Append(currentBuffer);

            ParseSingleEvent(eventBlock);
        }
    }

    // 보고서의 SSE 이벤트 명세에 맞춘 파싱 
    private void ParseSingleEvent(string eventBlock)
    {
        string eventName = "";
        string eventData = "";

        string[] lines = eventBlock.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("event:")) eventName = line.Substring(6).Trim();
            else if (line.StartsWith("data:")) eventData = line.Substring(5).Trim();
        }

        switch (eventName)
        {
            case "accepted":
                OnStatusUpdated?.Invoke("분석 대기 중...");
                break;
            case "audio_loaded":
            case "phoneme_completed": // ⭐️ Whisper(ASR) 단계가 빠지고 바로 음소 분석으로 넘어갑니다.
            case "g2p_completed":
            case "alignment_completed":
                OnStatusUpdated?.Invoke("발음 정밀 분석 중...");
                break;
            case "score":
                SSEEnvelope envelope = JsonUtility.FromJson<SSEEnvelope>(eventData);
                ScorePayload payload = envelope.data;

                Debug.Log($"🎯 최종 결과 - 정답 단어: {payload.recognized_word} / 점수: {payload.overall_score}점");
                OnStatusUpdated?.Invoke("분석 완료!");
                OnScoreReceived?.Invoke(payload);
                break;
            case "error":
                Debug.LogError($"[서버 거부 오류] {eventData}");
                OnStatusUpdated?.Invoke("음성 인식 실패: 다시 말해주세요!");
                break;
        }
    }
}