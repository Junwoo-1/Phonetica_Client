using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VoiceRecorder : MonoBehaviour
{
    // 1. 비주얼라이저가 접근할 수 있도록 싱글톤을 만듭니다.
    public static VoiceRecorder Instance { get; private set; }

    // 2. 현재 볼륨 수치를 외부에서 읽을 수 있게 공개합니다.
    public float CurrentVolume { get; private set; }

    [Header("마이크 설정")]
    private string _micName;
    private AudioClip _loopClip;
    private const int SAMPLE_RATE = 16000;

    [Header("VAD 감도 설정")]
    [Range(0f, 1f)] public float threshold = 0.05f;
    public float hangTime = 0.8f;
    public float preBufferTime = 0.3f;

    private bool _isCapturing = false;
    private float _silenceTimer = 0f;
    private int _lastMicPosition = 0;

    private List<float> _audioBuffer = new List<float>();

    // 3. 싱글톤 초기화
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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
        CurrentVolume = currentVolume;

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
            _silenceTimer += Time.unscaledDeltaTime;
            if (_silenceTimer > hangTime)
            {
                _isCapturing = false;

                byte[] wavData = ConvertToWav(_audioBuffer, SAMPLE_RATE);

                List<string> allCandidates = new List<string>();

                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    if (EnemySpawner.Instance != null)
                    {
                        allCandidates.AddRange(EnemySpawner.Instance.ActiveWords);
                    }
                }

                if (UIManager.Instance != null)
                {
                    allCandidates.AddRange(UIManager.Instance.GetActiveUICommands());
                }

                string candidatesStr = string.Join(",", allCandidates);
                Debug.Log($"[서버 전송] 후보 단어들: {candidatesStr}");

                PronunciationClient.Instance.SendAudioToServer(wavData, candidatesStr);
            }
        }

        if (_isCapturing)
        {
            ExtractAndBufferAudio(currentMicPos);
        }

        _lastMicPosition = currentMicPos;
    }

    private void ExtractPreBuffer(int currentMicPos, int sampleCount)
    {
        if (sampleCount <= 0 || _loopClip == null) return;
        int startPos = currentMicPos - sampleCount;

        if (startPos < 0)
        {
            startPos += _loopClip.samples;
            int firstPartSize = _loopClip.samples - startPos;
            float[] firstPart = new float[firstPartSize];
            _loopClip.GetData(firstPart, startPos);

            int secondPartSize = sampleCount - firstPartSize;
            float[] secondPart = new float[secondPartSize];
            if (secondPartSize > 0)
            {
                _loopClip.GetData(secondPart, 0);
            }

            _audioBuffer.AddRange(firstPart);
            _audioBuffer.AddRange(secondPart);
        }
        else
        {
            float[] preSamples = new float[sampleCount];
            _loopClip.GetData(preSamples, startPos);
            _audioBuffer.AddRange(preSamples);
        }
    }

    private void ExtractAndBufferAudio(int currentMicPos)
    {
        int sampleCount = currentMicPos - _lastMicPosition;
        if (sampleCount < 0)
        {
            sampleCount = _loopClip.samples - _lastMicPosition + currentMicPos;
        }

        if (sampleCount > 0)
        {
            float[] newSamples = new float[sampleCount];
            _loopClip.GetData(newSamples, _lastMicPosition);
            _audioBuffer.AddRange(newSamples);
        }
    }

    private float GetCurrentVolume(int currentMicPos)
    {
        int sampleSize = 256;
        float[] samples = new float[sampleSize];

        int startPosition = currentMicPos - sampleSize;
        if (startPosition < 0) return 0;

        _loopClip.GetData(samples, startPosition);

        float totalVolume = 0f;
        for (int i = 0; i < sampleSize; i++)
        {
            totalVolume += Mathf.Abs(samples[i]);
        }

        return totalVolume / sampleSize;
    }

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