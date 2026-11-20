using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Inspector에서 적 종류별로 다르게 설정할 값
    public float speed = 2f;     // 이동 속도
    public int maxHp = 3;        // 최대 체력 (WaveManager가 웨이브 배율을 곱함)
    public int goldReward = 15;  // 처치 보상 (기획서: 15골드)

    [Header("체력바")]
    public Vector2 hpBarOffset = new Vector2(0f, 0.55f);   // 적 중심에서 체력바까지의 위치
    public float hpBarWidth = 0.8f;
    public float hpBarHeight = 0.1f;

    int hp;                      // 현재 체력
    int wpIndex = 0;             // 지금 향하고 있는 웨이브포인트 번호
    Transform[] waypoints;       // 경로
    bool dead;                   // 처치되었거나 성에 도착해 제거가 예약된 상태

    public bool IsDead => dead;

    // 체력바 (모든 적이 같은 막대 스프라이트를 공유)
    static Sprite barSprite;
    GameObject barRoot;
    Transform barFill;
    SpriteRenderer fillSr;

    void Start()
    {
        hp = maxHp;
        waypoints = GameManager.Instance.waypoints;
        transform.position = waypoints[0].position;   // 시작 위치를 첫 웨이브포인트로
        CreateHpBar();
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
        if (dead) return;   // 이미 죽은 적에게는 피해, 골드, 카운트를 처리하지 않음

        hp -= dmg;
        UpdateHpBar();

        if (hp <= 0)
        {
            dead = true;
            GameManager.Instance.AddGold(goldReward);
            GameManager.Instance.OnEnemyRemoved();
            Destroy(gameObject);
        }
    }

    // ---------- 체력바 ----------
    void CreateHpBar()
    {
        // 흰색 4x4 텍스처로 '왼쪽 끝이 기준점인 1칸 너비 막대' 스프라이트를 한 번만 만듦
        if (barSprite == null)
            barSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                                      new Vector2(0f, 0.5f), 4f);

        int baseOrder = GetComponent<SpriteRenderer>().sortingOrder;

        // 적의 Scale이 1이 아니어도 체력바 크기가 일정하도록 보정
        Vector3 s = transform.localScale;
        barRoot = new GameObject("HpBar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        barRoot.transform.localPosition = new Vector3(hpBarOffset.x / s.x, hpBarOffset.y / s.y, 0f);

        MakeBar("Back", new Color(0f, 0f, 0f, 0.6f), baseOrder + 1);
        fillSr = MakeBar("Fill", Color.green, baseOrder + 2);
        barFill = fillSr.transform;

        UpdateHpBar();
    }

    SpriteRenderer MakeBar(string name, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(barRoot.transform, false);
        go.transform.localPosition = new Vector3(-hpBarWidth / 2f, 0f, 0f);   // 왼쪽 끝에서 시작
        go.transform.localScale = new Vector3(hpBarWidth, hpBarHeight, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = barSprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void UpdateHpBar()
    {
        if (barRoot == null) return;

        float ratio = Mathf.Clamp01((float)hp / maxHp);
        barFill.localScale = new Vector3(hpBarWidth * ratio, hpBarHeight, 1f);
        fillSr.color = Color.Lerp(Color.red, Color.green, ratio);   // 초록 → 노랑 → 빨강

        barRoot.SetActive(hp < maxHp);   // 한 번이라도 맞은 적만 표시
    }
}