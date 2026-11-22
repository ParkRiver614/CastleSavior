using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("전체 볼륨")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("배경음")]
    public AudioClip bgm;
    [Range(0f, 1f)] public float bgmVolume = 0.3f;

    [Header("발사")]
    public AudioClip shoot;
    [Range(0f, 1f)] public float shootVolume = 0.3f;
    public float shootMinInterval = 0.08f;

    [Header("적 처치")]
    public AudioClip kill;
    [Range(0f, 1f)] public float killVolume = 0.8f;

    [Header("성 피격")]
    public AudioClip castleHit;
    [Range(0f, 1f)] public float castleHitVolume = 0.8f;

    [Header("설치")]
    public AudioClip build;
    [Range(0f, 1f)] public float buildVolume = 0.8f;

    [Header("강화")]
    public AudioClip upgrade;
    [Range(0f, 1f)] public float upgradeVolume = 0.8f;

    [Header("승리 / 패배")]
    public AudioClip win;
    [Range(0f, 1f)] public float winVolume = 0.8f;
    public AudioClip lose;
    [Range(0f, 1f)] public float loseVolume = 0.8f;

    AudioSource source;     // 효과음용
    AudioSource bgmSource;  // 배경음용
    float lastShootTime = -1f;

    void Awake()
    {
        // 이미 살아 있는 AudioManager가 있으면 새로 생긴 쪽은 사라짐
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // 씬이 바뀌어도 유지

        source = GetComponent<AudioSource>();
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // 씬이 새로 열릴 때마다 호출됨 (메인 메뉴, 다시 시작 포함)
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;
        PlayBgm();
    }

    void Start()
    {
        if (Instance != this) return;
        PlayBgm();
    }

    void Update()
    {
        if (Instance != this) return;
        bgmSource.volume = bgmVolume * masterVolume;
    }

    // 멈춰 있을 때만 처음부터 재생, 이미 나오고 있으면 그대로 이어짐
    public void PlayBgm()
    {
        if (bgm == null || bgmSource.isPlaying) return;
        bgmSource.clip = bgm;
        bgmSource.Play();
    }

    public void PlayShoot()
    {
        if (Time.unscaledTime - lastShootTime < shootMinInterval) return;
        lastShootTime = Time.unscaledTime;
        Play(shoot, shootVolume);
    }

    public void PlayKill() { Play(kill, killVolume); }
    public void PlayCastleHit() { Play(castleHit, castleHitVolume); }
    public void PlayBuild() { Play(build, buildVolume); }
    public void PlayUpgrade() { Play(upgrade, upgradeVolume); }

    // 결과 화면: 배경음을 멈추고 징글 재생
    public void PlayWin() { bgmSource.Stop(); Play(win, winVolume); }
    public void PlayLose() { bgmSource.Stop(); Play(lose, loseVolume); }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        source.PlayOneShot(clip, volume * masterVolume);
    }
}