using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 어디서든 GameManager.Instance 로 접근하기 위한 변수 (싱글톤)
    public static GameManager Instance;

    // 적이 따라갈 경로 (Inspector에서 WP들을 넣어줌)
    public Transform[] waypoints;

    // 게임 상태 값 (기획서: 성 체력 5, 시작 골드 120)
    public int castleHp = 5;
    public int gold = 120;
    public int aliveEnemies = 0;

    // 게임 진행 상태
    public bool started = false;     // '게임 시작' 버튼을 눌렀는지
    public bool isPaused = false;    // 일시정지 중인지
    public bool isGameOver = false;
    public bool isCleared = false;

    // 지금 실제로 플레이 중인지 (입력과 웨이브 시작을 허용할지 판단)
    public bool IsPlaying => started && !isPaused && !isGameOver && !isCleared;

    void Awake()
    {
        Instance = this;
    }

    // 적이 성에 도착하면 호출됨
    public void DamageCastle(int amount)
    {
        if (isGameOver || isCleared) return;   // 이미 끝난 게임이면 무시

        castleHp -= amount;
        if (castleHp <= 0)
        {
            castleHp = 0;
            GameOver();
        }
    }

    // 적 처치 시 골드 획득
    public void AddGold(int amount)
    {
        gold += amount;
    }

    // 포탑 설치/강화 시 골드 사용. 부족하면 false 반환
    public bool TrySpend(int cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        return true;
    }

    // 적이 사라질 때(처치 또는 성 도착) 호출
    public void OnEnemyRemoved()
    {
        aliveEnemies--;
    }

    // 성 체력이 0이 되면 호출
    void GameOver()
    {
        isGameOver = true;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowResult(false);   // 결과 화면: 게임 오버
    }

    // 5웨이브를 모두 막으면 WaveManager가 호출
    public void Clear()
    {
        if (isGameOver || isCleared) return;
        isCleared = true;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowResult(true);    // 결과 화면: 게임 클리어
    }
}