using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 일시정지 화면과 결과 화면을 관리 (시작 화면은 StartScene이 담당)
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("화면 패널")]
    public GameObject pausePanel;
    public GameObject resultPanel;

    [Header("결과 화면 글자")]
    public TMP_Text resultTitleText;
    public TMP_Text resultDetailText;

    [Header("참조")]
    public WaveManager waveManager;

    [Header("씬 이름")]
    public string titleSceneName = "StartScene";

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 게임 씬은 열리면 바로 시작
        pausePanel.SetActive(false);
        resultPanel.SetActive(false);
        Time.timeScale = 1f;
        GameManager.Instance.started = true;
    }

    void Update()
    {
        // 기획서: ESC 키로 일시정지 메뉴를 열거나 닫는다
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // ESC 키, '일시정지' 버튼, '계속하기' 버튼
    public void TogglePause()
    {
        GameManager gm = GameManager.Instance;

        // 이미 끝났으면 일시정지 불가
        if (gm.isGameOver || gm.isCleared) return;

        gm.isPaused = !gm.isPaused;
        pausePanel.SetActive(gm.isPaused);
        Time.timeScale = gm.isPaused ? 0f : 1f;
    }

    // GameManager가 게임이 끝나면 호출
    public void ShowResult(bool cleared)
    {
        pausePanel.SetActive(false);
        resultPanel.SetActive(true);

        resultTitleText.text = cleared ? "게임 클리어" : "게임 오버";

        GameManager gm = GameManager.Instance;
        resultDetailText.text =
            $"도달한 웨이브: {waveManager.CurrentWave} / {waveManager.TotalWaves}\n" +
            $"남은 성 체력: {gm.castleHp}\n" +
            $"남은 골드: {gm.gold}";

        Time.timeScale = 0f;   // 결과 화면에서는 게임을 멈춤
    }

    // '다시 시작' 버튼: 게임 씬을 처음부터 다시 불러옴 (바로 게임 시작)
    public void Restart()
    {
        Time.timeScale = 1f;   // 멈춘 시간을 되돌린 뒤 씬을 불러와야 함
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // '메인 메뉴' 버튼: 시작 화면으로 돌아감
    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}