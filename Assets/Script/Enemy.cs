using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Inspector에서 적 종류별로 다르게 설정할 값
    public float speed = 2f;     // 이동 속도
    public int maxHp = 3;        // 최대 체력
    public int goldReward = 15;  // 처치 보상 (기획서: 15골드)

    int hp;                      // 현재 체력
    int wpIndex = 0;             // 지금 향하고 있는 웨이브포인트 번호
    Transform[] waypoints;       // 경로
    bool dead;                   // 처치되었거나 성에 도착해 제거가 예약된 상태

    public bool IsDead => dead;

    void Start()
    {
        hp = maxHp;
        waypoints = GameManager.Instance.waypoints;
        transform.position = waypoints[0].position;   // 시작 위치를 첫 웨이브포인트로
    }

    void Update()
    {
        if (dead) return;   // 제거가 예약된 적은 더 움직이지 않음

        // 지금 목표 지점을 향해 이동
        Transform target = waypoints[wpIndex];
        transform.position = Vector3.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime);

        // 목표에 거의 도착하면 다음 웨이브포인트로
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            wpIndex++;
            if (wpIndex >= waypoints.Length)
                ReachCastle();
        }
    }

    // 성까지 길을 따라 남은 거리 (값이 작을수록 성에 가까움)
    public float RemainingDistance()
    {
        if (waypoints == null || wpIndex >= waypoints.Length)
            return float.MaxValue;

        float d = Vector3.Distance(transform.position, waypoints[wpIndex].position);
        for (int i = wpIndex; i < waypoints.Length - 1; i++)
            d += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        return d;
    }

    void ReachCastle()
    {
        if (dead) return;
        dead = true;
        GameManager.Instance.DamageCastle(1);   // 성 체력 -1
        GameManager.Instance.OnEnemyRemoved();
        Destroy(gameObject);
    }

    // 포탑의 투사체가 맞으면 호출됨
    public void TakeDamage(int dmg)
    {
        if (dead) return;   // 이미 죽은 적에게는 피해, 골드, 카운트를 처리하지 않음 (핵심 수정)

        hp -= dmg;
        if (hp <= 0)
        {
            dead = true;
            GameManager.Instance.AddGold(goldReward);
            GameManager.Instance.OnEnemyRemoved();
            Destroy(gameObject);
        }
    }
}