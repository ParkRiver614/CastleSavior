using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8f;   // 투사체 이동 속도

    Enemy target;              // 따라갈 대상
    int damage;                // 포탑에게 받은 공격력

    // 포탑에서 발사할 때, 투사체에게 대상과 공격력을 넘겨줌
    public void Init(Enemy t, int dmg)
    {
        target = t;
        damage = dmg;
    }

    void Update()
    {
        // 적이 이미 죽었으면 투사체도 제거
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 대상 적을 향해 이동
        transform.position = Vector3.MoveTowards(
            transform.position, target.transform.position, speed * Time.deltaTime);

        // 닿으면 데미지를 주고 사라짐
        if (Vector3.Distance(transform.position, target.transform.position) < 0.1f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}