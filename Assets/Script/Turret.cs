using System.Collections;
using UnityEngine;

// 레벨 하나에 해당하는 데이터 (Inspector에서 레벨 순서대로 입력)
[System.Serializable]
public class TurretLevel
{
    public Sprite sprite;                // 이 레벨의 포탑 이미지
    public Projectile projectilePrefab;  // 이 레벨에서 발사하는 투사체
    public float scale = 1f;             // 이 레벨의 크기 보정 (이미지 크기 차이를 맞춤)
}

public class Turret : MonoBehaviour
{
    public const int MaxLevel = 3;       // 기획서: 최대 3레벨

    public float range = 2.5f;           // 사거리
    public float fireInterval = 1f;      // 발사 간격(초)
    public int level = 1;                // 현재 레벨 (1~3)
    public Vector2 fireOffset = new Vector2(0f, 0.9f);   // 투사체가 나가는 위치 (발밑 기준)

    [Header("강화 연출")]
    public Sprite ringSprite;                                   // 원 모양 스프라이트
    public Color upgradeColor = new Color(1f, 0.8f, 0.3f);      // 금빛

    [Header("레벨별 외형과 투사체 (1, 2, 3 순서)")]
    public TurretLevel[] levels;

    // 기획서: 레벨 1,2,3 → 공격력 1,2,3
    public int Damage => level;

    float timer;                         // 다음 발사까지 남은 시간
    SpriteRenderer sr;
    Color baseColor;
    Vector3 baseScale;
    bool isSelected;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;

        // 처음 크기를 기억 (0이면 1로 보정해서 포탑이 사라지지 않게)
        baseScale = transform.localScale;
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        UpdateVisual();                  // 1레벨 외형 적용
    }

    void Update()
    {
        // 발사 대기 시간 줄이기
        timer -= Time.deltaTime;
        if (timer > 0) return;

        // 사거리 안에서 성에 가장 가까운 적 찾기
        Enemy target = FindTarget();
        if (target == null) return;

        // 현재 레벨의 데이터(이미지·투사체·크기)를 꺼냄
        TurretLevel data = CurrentLevel();
        if (data == null || data.projectilePrefab == null) return;

        // 포탑 몸통 위치에서 현재 레벨의 투사체 발사
        Vector3 firePos = transform.position + (Vector3)fireOffset;
        Projectile p = Instantiate(data.projectilePrefab, firePos, Quaternion.identity);
        p.Init(target, Damage);

        timer = fireInterval;
    }

    // 현재 레벨의 데이터 (비어 있으면 null)
    TurretLevel CurrentLevel()
    {
        if (levels == null || levels.Length == 0) return null;
        return levels[Mathf.Clamp(level - 1, 0, levels.Length - 1)];
    }

    // 사거리 안의 적 중 '성에 가장 가까운' 적 (기획서: 성에 가까운 적 우선)
    Enemy FindTarget()
    {
        Enemy best = null;
        float bestRemain = float.MaxValue;

        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e.IsDead) continue;                  // 이미 죽은 적은 제외
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d > range) continue;                 // 사거리 밖은 제외

            float remain = e.RemainingDistance();    // 성까지 남은 경로 거리
            if (remain < bestRemain)
            {
                best = e;
                bestRemain = remain;
            }
        }
        return best;
    }

    // 강화: 레벨이 오르면 공격력(= 레벨)과 외형이 바뀜
    public void Upgrade()
    {
        if (level >= MaxLevel) return;
        level++;
        UpdateVisual();
        StopAllCoroutines();
        StartCoroutine(UpgradeEffect());
    }

    // 현재 레벨에 맞는 이미지와 크기 적용
    void UpdateVisual()
    {
        TurretLevel data = CurrentLevel();
        if (data == null) return;

        if (data.sprite != null)
            sr.sprite = data.sprite;

        float s = data.scale > 0f ? data.scale : 1f;   // 0이나 음수면 1로 취급
        transform.localScale = baseScale * s;
    }

    // 강화 연출: 금빛으로 두 번 깜빡이고, 발밑에서 빛이 퍼짐 (포탑 크기는 그대로)
    IEnumerator UpgradeEffect()
    {
        if (ringSprite != null) StartCoroutine(Ring());

        float duration = 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pulse = Mathf.Abs(Mathf.Sin(t / duration * Mathf.PI * 2f));   // 0→1→0→1→0 (두 번)
            sr.color = Color.Lerp(CurrentColor(), upgradeColor, pulse);
            yield return null;
        }
        sr.color = CurrentColor();
    }

    // 포탑 뒤에서 금빛 원이 커지며 사라짐
    IEnumerator Ring()
    {
        GameObject ring = new GameObject("UpgradeRing");
        ring.transform.position = transform.position + (Vector3)fireOffset * 0.5f;   // 포탑 몸통 가운데
        Destroy(ring, 0.6f);   // 도중에 포탑이 판매돼도 남지 않게

        SpriteRenderer rsr = ring.AddComponent<SpriteRenderer>();
        rsr.sprite = ringSprite;
        rsr.sortingOrder = sr.sortingOrder - 1;   // 포탑 뒤에 그려짐

        float duration = 0.4f;
        float t = 0f;
        while (t < duration && ring != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            ring.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.6f, k);
            rsr.color = new Color(upgradeColor.r, upgradeColor.g, upgradeColor.b, (1f - k) * 0.8f);
            yield return null;
        }
    }

    // 선택 여부에 맞는 원래 색
    Color CurrentColor()
    {
        return isSelected ? new Color(1f, 1f, 0.6f, baseColor.a) : baseColor;
    }

    // 선택되면 살짝 노랗게 표시
    public void SetSelected(bool on)
    {
        isSelected = on;
        sr.color = CurrentColor();
    }

    // 에디터에서 포탑을 선택하면 사거리를 원으로 보여줌
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, range);
    }

}