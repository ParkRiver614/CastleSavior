using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public int maxHp = 3;
    public int goldReward = 15;

    int hp;
    int wpIndex = 0;
    Transform[] waypoints;

    void Start()
    {
        hp = maxHp;
        waypoints = GameManager.Instance.waypoints;
        transform.position = waypoints[0].position;
    }

    void Update()
    {
        Transform target = waypoints[wpIndex];
        transform.position = Vector3.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            wpIndex++;
            if (wpIndex >= waypoints.Length)
                ReachCastle();
        }
    }

    void ReachCastle()
    {
        GameManager.Instance.DamageCastle(1);
        GameManager.Instance.OnEnemyRemoved();
        Destroy(gameObject);
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0)
        {
            GameManager.Instance.AddGold(goldReward);
            GameManager.Instance.OnEnemyRemoved();
            Destroy(gameObject);
        }
    }
}
