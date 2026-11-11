using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8f;           // 투사체 이동 속도
    public float rotationOffset = 0f;  // 스프라이트 원래 방향 보정값

    Enemy target;                      // 따라갈 대상
    int damage;                        // 포탑에게 받은 공격력

    // 포탑이 발사할 때 대상과 공격력을 넘겨줌
    public void Init(Enemy t, int dmg)
    {
        target = t;
        damage = dmg;
        RotateToTarget();              // 생성되는 순간부터 대상을 바라보게
    }

    void Update()
    {
        // 대상이 이미 죽었으면 투사체도 사라짐
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        RotateToTarget();              // 적이 움직이므로 매 프레임 다시 조준

        // 대상을 향해 이동
        transform.position = Vector3.MoveTowards(
            transform.position, target.transform.position, speed * Time.deltaTime);

        // 닿으면 데미지를 주고 사라짐
        if (Vector3.Distance(transform.position, target.transform.position) < 0.1f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    // 대상이 있는 방향으로 스프라이트를 회전시킴
    void RotateToTarget()
    {
        if (target == null) return;

        Vector3 dir = target.transform.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;   // 거의 겹쳤으면 회전 생략

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }
}