using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class JamoToken
{
    public string @char;
    public string pos;
    public int syl; // ⭐️ string에서 int로 변경! (서버에서 0, 1 같은 정수로 보냄)
}

[Serializable]
public class SSEEnvelope
{
    public string @event;
    public ScorePayload data; // 이 안에 진짜 데이터가 들어있습니다.
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
    public PositionAccuracy onset;   // 초성 (ㄱ, ㄷ, ㅂ...)
    public PositionAccuracy nucleus; // 중성 (ㅏ, ㅗ, ㅜ...)
    public PositionAccuracy coda;    // 종성 (받침)
}

[Serializable]
public class ScorePayload
{
    public float overall_score;
    public string recognized_word;
    public JamoToken[] ref_jamo;

    // ⭐️ 서버가 주는 진짜 발음 분석 데이터를 추가로 받습니다!
    public PerPosition per_position;
    public string[] problem_jamos;
}


public class PronunciationClient : MonoBehaviour
{
    public static PronunciationClient Instance { get; private set; }

    [Header("서버 설정")]
    // 로컬 테스트 시 http://127.0.0.1:8000/pronounce, 실서버면 해당 IP 입력
    public string serverUrl = "http://localhost:8000/pronounce"; 

    // 외부(Player 스크립트 등)에서 결과를 받기 위한 이벤트
    public event Action<ScorePayload> OnScoreReceived; // 최종 점수 수신 이벤트
    public event Action<string> OnStatusUpdated; // UI에 상태(로딩)를 띄우기 위한 이벤트

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // VAD 녹음이 끝나면 이 함수를 호출하여 wavData를 넘겨줍니다.
    public void SendAudioToServer(byte[] wavData)
    {
        Debug.Log("[PronunciationClient] SendAudioToServer 함수가 호출되었습니다!");
        StartCoroutine(UploadAndReceiveSSE(wavData));
    }

    private IEnumerator UploadAndReceiveSSE(byte[] wavData)
    {
        OnStatusUpdated?.Invoke("서버 연결 중...");

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "voice.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            request.SetRequestHeader("Accept", "text/event-stream");

            // 파싱된 이벤트가 들어오면 기존에 만들어둔 ParseSingleEvent 함수를 바로 실행하라고 연결해 줍니다.
            request.downloadHandler = new SSEDownloadHandler(ParseSingleEvent);

            // 요청 전송
            yield return request.SendWebRequest(); // 코루틴이 알아서 끝날 때까지 여기서 대기합니다.

            // 통신이 완전히 끝났을 때 에러 체크
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
        Debug.Log($"[수신 데이터 확인] {eventBlock}");
        string eventName = "";
        string eventData = "";

        string[] lines = eventBlock.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("event:")) eventName = line.Substring(6).Trim();
            else if (line.StartsWith("data:")) eventData = line.Substring(5).Trim();
        }

        Debug.Log($"[SSE 수신] Event: {eventName}");

        switch (eventName)
        {
            case "accepted":
                OnStatusUpdated?.Invoke("오디오 전송 완료. 분석 대기 중...");
                break;
            case "audio_loaded":
                OnStatusUpdated?.Invoke("오디오 로드 완료 (STEP 1)..."); // [cite: 15]
                break;
            case "asr_progress":
                OnStatusUpdated?.Invoke("음성 텍스트 변환 중 (STEP 2)..."); // [cite: 21]
                break;
            case "asr_completed":
            case "g2p_completed":
            case "phoneme_completed":
            case "alignment_completed":
                OnStatusUpdated?.Invoke("발음 정밀 분석 중..."); // 상세 단계 묶음 처리 
                break;
            case "score":
                // 1. 껍데기(Envelope) 클래스로 먼저 JSON을 파싱합니다.
                SSEEnvelope envelope = JsonUtility.FromJson<SSEEnvelope>(eventData);

                // 2. 알맹이(data)만 빼옵니다.
                ScorePayload payload = envelope.data;

                // 3. 만약 서버에서 recognized_word가 오지 않았다면 ref_jamo로 복원합니다.
                // (syl이 이제 int가 되었으므로 로직 수정이 필요합니다)
                if (string.IsNullOrEmpty(payload.recognized_word) && payload.ref_jamo != null)
                {
                    // 서버가 recognized_word를 보내주므로 이 로직을 탈 일은 거의 없지만,
                    // 안전장치를 위해 남겨둔다면 구조를 바꿔야 합니다. 
                    // 하지만 현재 서버는 확실히 recognized_word를 줍니다.
                }

                // 4. 로그 출력 및 이벤트 발생
                Debug.Log($"🎯 최종 결과 - 발음: {payload.recognized_word} / 점수: {payload.overall_score}점");
                OnStatusUpdated?.Invoke("분석 완료!");
                OnScoreReceived?.Invoke(payload);
                break;
            case "done":
                Debug.Log("[SSE] 스트림 정상 종료"); // 
                break;
            case "error":
                // NO_SPEECH, NO_HANGUL 또는 95% 이상 무음 [cite: 18, 26]
                Debug.LogError($"[서버 거부 오류] {eventData}");
                OnStatusUpdated?.Invoke("음성 인식 실패: 다시 말해주세요!");
                break;
        }
    }
}