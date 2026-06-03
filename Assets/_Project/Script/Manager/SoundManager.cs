using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // 어디서든 SoundManager.Instance 로 접근할 수 있게 만듭니다.
    public static SoundManager Instance { get; private set; }

    [Header("오디오 플레이어 (Audio Source)")]
    public AudioSource bgmSource; // BGM을 재생할 카세트테이프
    public AudioSource sfxSource; // 효과음을 재생할 카세트테이프

    [Header("BGM 음악 파일")]
    public AudioClip mainBGM;     // 인스펙터에서 넣을 BGM 파일

    private void Awake()
    {
        // 싱글톤 세팅 및 씬 전환 시 파괴 방지 (음악이 끊기지 않게!)
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 매니저가 파괴되지 않게 보호합니다.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임이 시작되면 등록된 메인 BGM을 틀어줍니다.
        if (mainBGM != null)
        {
            PlayBGM(mainBGM);
        }
    }

    // BGM 재생 함수
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = true; // BGM은 무한 반복 필수!
        bgmSource.volume = 0.5f; // 기본 볼륨 50% (원하는 대로 조절하세요)
        bgmSource.Play();
    }

    // BGM 정지 함수 (게임 오버 시 음악을 끌 때 유용합니다)
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // 효과음(SFX) 재생 함수 (나중을 위한 보너스!)
    // 사용법 예시: SoundManager.Instance.PlaySFX(hitSound);
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (sfxSource == null || clip == null) return;

        // PlayOneShot은 소리가 겹쳐도 끊기지 않고 덧씌워져서 재생됩니다. (타격음에 필수!)
        sfxSource.PlayOneShot(clip, volume);
    }

    public void SetVolume(float volume)
    {
        if (bgmSource != null) bgmSource.volume = volume;
        if (sfxSource != null) sfxSource.volume = volume;

        Debug.Log($"[SoundManager] 전체 볼륨이 {volume * 100}% 로 설정되었습니다.");
    }
}