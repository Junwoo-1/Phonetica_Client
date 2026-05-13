using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 백엔드에서 날아올 JSON 구조 중 'score' 데이터를 담을 그릇 (보고서 기준)
[Serializable]
public class ScorePayload
{
    public float overall_score; // 0~100 점수 (데미지 계수로 사용)
    public float per;
    public float weighted_per;
    public bool low_confidence;
    public string recognized_word;
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
                // JSON 파싱: overall_score 추출
                ScorePayload payload = JsonUtility.FromJson<ScorePayload>(eventData);
                Debug.Log($"🎯 최종 점수 획득: {payload.overall_score}점!");
                OnStatusUpdated?.Invoke("분석 완료!");
                
                // Player 스크립트 등에게 점수를 뿌려줍니다.
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