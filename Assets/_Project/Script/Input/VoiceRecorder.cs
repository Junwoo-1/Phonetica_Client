using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ContinuousVoiceRecorder : MonoBehaviour
{
    [Header("마이크 설정")]
    private string _micName;
    private AudioClip _loopClip;
    private const int SAMPLE_RATE = 16000;

    [Header("UI 시각화")]
    public Slider volumeSlider;

    [Header("VAD 감도 설정")]
    [Range(0f, 1f)] public float threshold = 0.05f;
    public float hangTime = 0.8f;
    public float preBufferTime = 0.3f;

    private bool _isCapturing = false;
    private float _silenceTimer = 0f;
    private int _lastMicPosition = 0;

    private List<float> _audioBuffer = new List<float>();

    // ⭐️ 추가: UI 제어용 시스템 단어 목록
    private List<string> _uiCommands = new List<string> { "일시정지", "메뉴" };

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            _micName = Microphone.devices[0];
            _loopClip = Microphone.Start(_micName, true, 10, SAMPLE_RATE);
            Debug.Log($"[VAD] 상시 감지 대기 중... (Threshold: {threshold})");
        }
    }

    void Update()
    {
        if (_micName == null || !Microphone.IsRecording(_micName)) return;

        int currentMicPos = Microphone.GetPosition(_micName);
        if (currentMicPos == _lastMicPosition) return;

        float currentVolume = GetCurrentVolume(currentMicPos);

        if (volumeSlider != null) volumeSlider.value = currentVolume;

        if (currentVolume > threshold)
        {
            _silenceTimer = 0f;
            if (!_isCapturing)
            {
                _isCapturing = true;
                _audioBuffer.Clear();

                int preBufferSamples = Mathf.FloorToInt(preBufferTime * SAMPLE_RATE);
                ExtractPreBuffer(currentMicPos, preBufferSamples);

                Debug.Log($"🎙️ [녹음 시작] 목소리 감지됨!");
            }
        }
        else if (_isCapturing)
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer > hangTime)
            {
                _isCapturing = false;

                byte[] wavData = ConvertToWav(_audioBuffer, SAMPLE_RATE);

                // ⭐️ [NEW] 하이브리드 통신: 서버로 보낼 후보군 문자열 조립
                List<string> allCandidates = new List<string>();

                // 1. 화면에 스폰된 몬스터 단어들 추가
                if (EnemySpawner.Instance != null)
                {
                    allCandidates.AddRange(EnemySpawner.Instance.ActiveWords);
                }

                // 2. UI 명령어 추가
                allCandidates.AddRange(_uiCommands);

                // 3. 콤마(,)로 묶어서 하나의 문자열로 만들기
                string candidatesStr = string.Join(",", allCandidates);
                Debug.Log($"[서버 전송] 후보 단어들: {candidatesStr}");

                // 4. 오디오와 후보군을 함께 서버로 전송!
                PronunciationClient.Instance.SendAudioToServer(wavData, candidatesStr);
            }
        }

        if (_isCapturing)
        {
            ExtractAndBufferAudio(currentMicPos);
        }

        _lastMicPosition = currentMicPos;
    }

    // ⭐️ 새로 추가된 함수: 감지된 시점보다 과거의 소리를 AudioClip에서 가져옵니다.
    private void ExtractPreBuffer(int currentMicPos, int sampleCount)
    {
        if (sampleCount <= 0 || _loopClip == null) return;

        // 과거로 돌아갈 시작 위치 계산
        int startPos = currentMicPos - sampleCount;

        if (startPos < 0)
        {
            // 마이크 루프의 처음(0)을 넘어 끝부분으로 돌아간 경우 (Wrap-around)
            startPos += _loopClip.samples;

            // 1. 클립의 끝부분(startPos ~ 끝) 가져오기
            int firstPartSize = _loopClip.samples - startPos;
            float[] firstPart = new float[firstPartSize];
            _loopClip.GetData(firstPart, startPos);

            // 2. 클립의 앞부분(0 ~ 남은 개수) 가져오기
            int secondPartSize = sampleCount - firstPartSize;
            float[] secondPart = new float[secondPartSize];
            if (secondPartSize > 0)
            {
                _loopClip.GetData(secondPart, 0);
            }

            // 바구니에 순서대로 담기
            _audioBuffer.AddRange(firstPart);
            _audioBuffer.AddRange(secondPart);
        }
        else
        {
            // 루프 경계를 넘지 않는 평범한 경우
            float[] preSamples = new float[sampleCount];
            _loopClip.GetData(preSamples, startPos);
            _audioBuffer.AddRange(preSamples);
        }
    }

    // 새로 추가된 함수: 마이크 클립에서 새 소리 조각을 떼어내어 바구니에 담기
    private void ExtractAndBufferAudio(int currentMicPos)
    {
        int sampleCount = currentMicPos - _lastMicPosition;
        if (sampleCount < 0) 
        {
            // 마이크 루프가 한 바퀴 돌아서 위치가 0으로 돌아갔을 때의 예외 처리
            sampleCount = _loopClip.samples - _lastMicPosition + currentMicPos;
        }

        if (sampleCount > 0)
        {
            float[] newSamples = new float[sampleCount];
            _loopClip.GetData(newSamples, _lastMicPosition);
            _audioBuffer.AddRange(newSamples); // 바구니에 쏟아 붓기
        }
    }

    // 소리의 크기(진폭)를 계산하는 함수
    private float GetCurrentVolume(int currentMicPos)
    {
        // 256개의 샘플(소리 조각)만 가져와서 검사합니다.
        int sampleSize = 256; 
        float[] samples = new float[sampleSize];
        
        // 현재 위치에서 256만큼 뒤로 간 위치의 소리를 읽어옵니다. (에러 방지용 수학 처리 포함)
        int startPosition = currentMicPos - sampleSize;
        if (startPosition < 0) return 0; // 루프가 한 바퀴 돌 때의 복잡한 예외 처리 임시 방편

        _loopClip.GetData(samples, startPosition);

        // 읽어온 소리들의 절대값 평균을 구합니다. (RMS와 유사)
        float totalVolume = 0f;
        for (int i = 0; i < sampleSize; i++)
        {
            totalVolume += Mathf.Abs(samples[i]);
        }
        
        return totalVolume / sampleSize;
    }

    // List<float> 형태의 오디오 데이터를 표준 WAV 파일 규격(byte 배열)으로 변환
    private byte[] ConvertToWav(List<float> samples, int sampleRate = 16000)
    {
        int headerSize = 44;
        int byteRate = sampleRate * 2;
        byte[] wavData = new byte[headerSize + samples.Count * 2];

        System.Text.Encoding.UTF8.GetBytes("RIFF").CopyTo(wavData, 0);
        System.BitConverter.GetBytes(36 + samples.Count * 2).CopyTo(wavData, 4);
        System.Text.Encoding.UTF8.GetBytes("WAVE").CopyTo(wavData, 8);
        System.Text.Encoding.UTF8.GetBytes("fmt ").CopyTo(wavData, 12);
        System.BitConverter.GetBytes(16).CopyTo(wavData, 16);
        System.BitConverter.GetBytes((short)1).CopyTo(wavData, 20);
        System.BitConverter.GetBytes((short)1).CopyTo(wavData, 22);
        System.BitConverter.GetBytes(sampleRate).CopyTo(wavData, 24);
        System.BitConverter.GetBytes(byteRate).CopyTo(wavData, 28);
        System.BitConverter.GetBytes((short)2).CopyTo(wavData, 32);
        System.BitConverter.GetBytes((short)16).CopyTo(wavData, 34);
        System.Text.Encoding.UTF8.GetBytes("data").CopyTo(wavData, 36);
        System.BitConverter.GetBytes(samples.Count * 2).CopyTo(wavData, 40);

        int offset = 44;
        foreach (float sample in samples)
        {
            short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
            System.BitConverter.GetBytes(intSample).CopyTo(wavData, offset);
            offset += 2;
        }
        return wavData;
    }
}