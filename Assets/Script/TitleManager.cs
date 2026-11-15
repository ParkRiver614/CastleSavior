using UnityEngine;
using UnityEngine.SceneManagement;

// 시작 화면(StartScene)을 관리
public class TitleManager : MonoBehaviour
{
    public string gameSceneName = "Gamescene";   // 이동할 게임 씬 이름

    void Start()
    {
        Time.timeScale = 1f;   // 게임 씬에서 돌아왔을 때 멈춘 시간을 되돌림
    }

    // '게임 시작' 버튼
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // '종료' 버튼 (선택)
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // 에디터에서는 실행 중지
#else
        Application.Quit();                                // 빌드에서는 프로그램 종료
#endif
    }
}