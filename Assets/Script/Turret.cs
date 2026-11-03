using UnityEngine;

public class Turret : MonoBehaviour
{
    public float range = 2.5f;           // 사거리
    public float fireInterval = 1f;      // 발사 간격(초)
    public int level = 1;                // 현재 레벨
    public Projectile projectilePrefab;  // 발사할 투사체 프리팹

    public int Damage => level;     // 레벨 당 공격력 증가

    float timer;    // 다음 발사까지 남은 시간

    void Update()
    {
        // 발사 대기 시간 줄이기
        timer -= Time.deltaTime;
        if (timer > 0) return;

        // 사거리 안의 적 찾기
        Enemy target = FindTarget();
        if (target == null) return;

        // 투사체 생성 후 투사체에 공격 대상과 데미지 넘기기
        Projectile p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        p.Init(target, Damage);

        timer = fireInterval;   // 대기 시간 다시 채우기
    }

    // 사거리 안에서 가장 가까운 적을 반환
    Enemy FindTarget()
    {
        Enemy best = null;
        float bestDist = range;

        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d <= bestDist)
            {
                best = e;
                bestDist = d;
            }
        }
        return best;
    }

    // 포탑 선택시 해당 포탑 사거리 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, range);
    }
}