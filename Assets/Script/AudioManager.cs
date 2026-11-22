using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("전체 볼륨")]
    [Range(0f, 1f)] public float masterVolume = 1f;

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

    AudioSource source;
    float lastShootTime = -1f;

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
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
    public void PlayWin() { Play(win, winVolume); }
    public void PlayLose() { Play(lose, loseVolume); }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        source.PlayOneShot(clip, volume * masterVolume);
    }
}