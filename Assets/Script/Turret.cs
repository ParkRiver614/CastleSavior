using System.Collections;
using UnityEngine;

// 레벨 하나에 해당하는 데이터 (Inspector에서 레벨 순서대로 입력)
[System.Serializable]
public class TurretLevel
{
    public Sprite sprite;                // 이 레벨의 포탑 이미지
    public Projectile projectilePrefab;  // 이 레벨에서 발사하는 투사체
}

public class Turret : MonoBehaviour
{
    public const int MaxLevel = 3;       // 기획서: 최대 3레벨

    public float range = 2.5f;           // 사거리
    public float fireInterval = 1f;      // 발사 간격(초)
    public int level = 1;                // 현재 레벨 (1~3)

    [Header("레벨별 외형과 투사체 (1, 2, 3 순서)")]
    public TurretLevel[] levels;

    // 기획서: 레벨 1,2,3 → 공격력 1,2,3
    public int Damage => level;

    float timer;                         // 다음 발사까지 남은 시간
    SpriteRenderer sr;
    Color baseColor;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        baseScale = transform.localScale;
        UpdateVisual();                  // 1레벨 이미지 적용
    }

    void Update()
    {
        // 발사 대기 시간 줄이기
        timer -= Time.deltaTime;
        if (timer > 0) return;

        // 사거리 안의 적 찾기
        Enemy target = FindTarget();
        if (target == null) return;

        // 현재 레벨의 투사체를 생성
        Projectile prefab = CurrentProjectile();
        if (prefab == null) return;

        Projectile p = Instantiate(prefab, transform.position, Quaternion.identity);
        p.Init(target, Damage);

        timer = fireInterval;
    }

    // 현재 레벨의 투사체 프리팹 (데이터가 비어 있으면 null)
    Projectile CurrentProjectile()
    {
        if (levels == null || levels.Length == 0) return null;
        int index = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        return levels[index].projectilePrefab;
    }

    // 사거리 안에서 가장 가까운 적을 반환
    // 사거리 안의 적 중 '성에 가장 가까운' 적을 반환 (기획서: 성에 가까운 적 우선)
    Enemy FindTarget()
    {
        Enemy best = null;
        float bestRemain = float.MaxValue;

        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e.IsDead) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d > range) continue;                // 사거리 밖은 후보에서 제외

            float remain = e.RemainingDistance();   // 성까지 남은 경로 거리
            if (remain < bestRemain)
            {
                best = e;
                bestRemain = remain;
            }
        }
        return best;
    }

    // 강화: 레벨이 오르면 공격력(= 레벨)도 자동으로 오르고 외형이 바뀜
    public void Upgrade()
    {
        if (level >= MaxLevel) return;
        level++;
        UpdateVisual();
        StopAllCoroutines();
        StartCoroutine(UpgradeEffect());
    }

    // 현재 레벨에 맞는 이미지로 교체
    void UpdateVisual()
    {
        if (levels == null || levels.Length == 0) return;

        int index = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        if (levels[index].sprite != null)
            sr.sprite = levels[index].sprite;
    }

    // 강화 연출: 잠깐 커졌다가 돌아옴
    IEnumerator UpgradeEffect()
    {
        float duration = 0.3f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float pop = 1f + 0.3f * Mathf.Sin(t / duration * Mathf.PI);  // 1 → 1.3 → 1
            transform.localScale = baseScale * pop;
            yield return null;
        }
        transform.localScale = baseScale;
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