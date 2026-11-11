using UnityEngine;

public class Turret : MonoBehaviour
{
    public const int MaxLevel = 3;       // 기획서: 최대 3레벨

    public float range = 2.5f;           // 사거리
    public float fireInterval = 1f;      // 발사 간격(초)
    public int level = 1;                // 현재 레벨 (1~3)
    public Projectile projectilePrefab;  // 발사할 투사체 프리팹
    public Sprite[] levelSprites;        // 레벨별 이미지 (비워 둬도 됨)

    // 기획서: 레벨 1,2,3 → 공격력 1,2,3
    public int Damage => level;

    float timer;                         // 다음 발사까지 남은 시간
    SpriteRenderer sr;
    Color baseColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    void Update()
    {
        // 발사 대기 시간 줄이기
        timer -= Time.deltaTime;
        if (timer > 0) return;

        // 사거리 안의 적 찾기
        Enemy target = FindTarget();
        if (target == null) return;

        // 투사체 생성 후 대상과 공격력을 알려줌
        Projectile p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        p.Init(target, Damage);

        timer = fireInterval;
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

    // 강화: 레벨이 오르면 공격력(= 레벨)도 자동으로 오름
    public void Upgrade()
    {
        if (level >= MaxLevel) return;
        level++;
        UpdateVisual();
    }

    // 레벨에 맞는 스프라이트로 교체 (levelSprites를 채웠을 때만)
    void UpdateVisual()
    {
        if (levelSprites != null && levelSprites.Length >= level)
            sr.sprite = levelSprites[level - 1];
    }

    // 선택되면 살짝 노랗게 표시
    public void SetSelected(bool on)
    {
        sr.color = on ? new Color(1f, 1f, 0.6f, baseColor.a) : baseColor;
    }

    // 에디터에서 포탑을 선택하면 사거리를 원으로 보여줌
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, range);
    }
}