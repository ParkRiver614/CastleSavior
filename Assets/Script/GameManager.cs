using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;     // 다른 cs파일에서 접근 할 수 있도록 객체 생성
    public Transform[] waypoints;   // 적이 이동할 경로

    public int castleHp = 5;
    public int gold = 120;      // 기본 값 (hp:5, 골드:120)
    public int aliveEnemies = 0;

    void Awake() { Instance = this; }

    public void DamageCastle(int amount)
    {
        castleHp -= amount;     // 적이 성과 충돌 시 Hp 감소
        if (castleHp <= 0) { castleHp = 0; }    // Hp가 0 이하이면 게임 오버
    }

    public void AddGold(int amount) { gold += amount; }     // 적 처치 시 골드 획득

    public bool TrySpend(int cost)      // 포탑 설치/강화 시 골드 사용
    {
        if (gold < cost) return false;      // 돈이 부족하면 False 리턴
        gold -= cost;
        return true;
    }

    public void OnEnemyRemoved() { aliveEnemies--; }    // 적이 처치 되거나 성에 도착했을 때 적 수 제거
}