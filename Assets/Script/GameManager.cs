using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Transform[] waypoints;

    public int castleHp = 5;
    public int gold = 120;
    public int aliveEnemies = 0;

    void Awake() { Instance = this; }

    public void DamageCastle(int amount)
    {
        castleHp -= amount;
        if (castleHp <= 0) { castleHp = 0; /* 게임 오버 처리 */ }
    }

    public void AddGold(int amount) { gold += amount; }

    public bool TrySpend(int cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        return true;
    }

    public void OnEnemyRemoved() { aliveEnemies--; }
}