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
    [Range(0f, 1f)] public float threshold = 0.05f; // 소리 감지 기준점 (유저마다 다름!)
    public float hangTime = 0.8f; // 말이 끝나고 몇 초 뒤에 녹음을 전송할지
    public float preBufferTime = 0.3f; // ⭐️ 추가: 녹음 시작 전 보존할 시간(초)

    private bool _isCapturing = false;
    private float _silenceTimer = 0f;
    private int _lastMicPosition = 0;

    // 녹음된 오디오 샘플을 모아둘 바구니
    private List<float> _audioBuffer = new List<float>(); 

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            _micName = Microphone.devices[0];
            // 마이크를 '루프 모드(true)'로 10초짜리 클립에 무한히 덮어쓰며 녹음 시작
            _loopClip = Microphone.Start(_micName, true, 10, SAMPLE_RATE);
            Debug.Log($"[VAD] 상시 감지 대기 중... (Threshold: {threshold})");
        }
    }

    void Update()
    {
        if (_micName == null || !Microphone.IsRecording(_micName)) return;

        int currentMicPos = Microphone.GetPosition(_micName);
        if (currentMicPos == _lastMicPosition) return;

        // 소리 크기 계산
        float currentVolume = GetCurrentVolume(currentMicPos);

        // 1. UI 슬라이더에 현재 볼륨 표시
        if (volumeSlider != null)
        {
            volumeSlider.value = currentVolume;
        }

        // --- VAD 상태 로직 ---
        if (currentVolume > threshold)
        {
            _silenceTimer = 0f;
            if (!_isCapturing)
            {
                _isCapturing = true;
                _audioBuffer.Clear();

                // ⭐️ 추가: 녹음 시작 시, 과거 0.3초 분량의 소리를 미리 버퍼에 담습니다.
                int preBufferSamples = Mathf.FloorToInt(preBufferTime * SAMPLE_RATE);
                ExtractPreBuffer(currentMicPos, preBufferSamples);

                Debug.Log($"🎙️ [녹음 시작] 목소리 감지됨! (프리버퍼 확보 완료)");
            }
        }
        else if (_isCapturing)
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer > hangTime)
            {
                _isCapturing = false;
                Debug.Log($"[녹음 완료] 버퍼 샘플 수: {_audioBuffer.Count}");
                
                // 2단계에서 만들 WAV 변환 함수 호출!
                byte[] wavData = ConvertToWav(_audioBuffer, SAMPLE_RATE);
                
                // 서버 사이드로 송신
                PronunciationClient.Instance.SendAudioToServer(wavData);
            }
        }

        // 2. 캡처 중일 때만 실제 오디오 데이터를 리스트에 차곡차곡 저장합니다.
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
        int byteRate = sampleRate * 2; // 16-bit(2 bytes) Mono(1 channel)
        byte[] wavData = new byte[headerSize + samples.Count * 2];

        // --- 1. WAV RIFF 헤더 44바이트 작성 (국제 표준 규격) ---
        System.Text.Encoding.UTF8.GetBytes("RIFF").CopyTo(wavData, 0);
        System.BitConverter.GetBytes(36 + samples.Count * 2).CopyTo(wavData, 4); // 전체 파일 크기
        System.Text.Encoding.UTF8.GetBytes("WAVE").CopyTo(wavData, 8);
        
        System.Text.Encoding.UTF8.GetBytes("fmt ").CopyTo(wavData, 12);
        System.BitConverter.GetBytes(16).CopyTo(wavData, 16); // fmt 청크 크기
        System.BitConverter.GetBytes((short)1).CopyTo(wavData, 20); // 오디오 포맷 (1 = PCM)
        System.BitConverter.GetBytes((short)1).CopyTo(wavData, 22); // 채널 수 (1 = Mono)
        System.BitConverter.GetBytes(sampleRate).CopyTo(wavData, 24); // 샘플 레이트 (16000)
        System.BitConverter.GetBytes(byteRate).CopyTo(wavData, 28); // 바이트 레이트
        System.BitConverter.GetBytes((short)2).CopyTo(wavData, 32); // 블록 얼라인
        System.BitConverter.GetBytes((short)16).CopyTo(wavData, 34); // 비트 뎁스 (16-bit)
        
        System.Text.Encoding.UTF8.GetBytes("data").CopyTo(wavData, 36);
        System.BitConverter.GetBytes(samples.Count * 2).CopyTo(wavData, 40); // 데이터 청크 크기

        // --- 2. 실제 오디오 데이터 변환 ---
        // 유니티의 float (-1.0 ~ 1.0)을 16-bit PCM 정수 (-32768 ~ 32767)로 변환합니다.
        int offset = 44;
        foreach (float sample in samples)
        {
            // 노이즈가 튀는 것을 막기 위해 Clamp로 값을 제한한 뒤 16비트로 변환
            short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
            System.BitConverter.GetBytes(intSample).CopyTo(wavData, offset);
            offset += 2;
        }

        Debug.Log($"[WAV 변환 완료] {wavData.Length} 바이트 생성됨.");
        return wavData;
    }
}