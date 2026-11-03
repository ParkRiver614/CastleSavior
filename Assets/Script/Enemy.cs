using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;    // 속도
    public int maxHp = 3;       // 최대 체력
    public int goldReward = 15;     // 처치 시 보상

    int hp;     // 현재 체력
    int wpIndex = 0;        // 향하는 웨이포인트 번호
    Transform[] waypoints;

    void Start()
    {
        hp = maxHp;     // 체력 설정
        waypoints = GameManager.Instance.waypoints;
        transform.position = waypoints[0].position;     // 시작 포지션을 WP0로 설정
    }

    void Update()
    {
        Transform target = waypoints[wpIndex];      // 다음 WP로 이동
        transform.position = Vector3.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            wpIndex++;      // WP에 거의 도착하면 다음으로 이동해야 할 WP 설정
            if (wpIndex >= waypoints.Length)
                ReachCastle();      // 마지막 WP에 도착하면 성에 도착한 것으로 판단
        }
    }

    void ReachCastle()
    {
        GameManager.Instance.DamageCastle(1);   // 성 체력 -1 감소
        GameManager.Instance.OnEnemyRemoved();      // 성에 닿은 적 제거
        Destroy(gameObject);
    }

    public void TakeDamage(int dmg)     // 적이 포탑 투사체에 맞으면 호출
    {
        hp -= dmg;
        if (hp <= 0)    // 적 Hp가 0 이하일 경우 보상 지급 및 적 제거
        {
            GameManager.Instance.AddGold(goldReward);
            GameManager.Instance.OnEnemyRemoved();
            Destroy(gameObject);
        }
    }
}
