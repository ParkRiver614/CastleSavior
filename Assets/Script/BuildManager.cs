using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class BuildManager : MonoBehaviour
{
    [Header("포탑 규칙 (기획서 수치)")]
    public Turret turretPrefab;
    public int buildCost = 60;
    public int[] upgradeCosts = { 80, 120 };   // 1→2레벨, 2→3레벨
    public float sellRefundRate = 0.5f;        // 포탑 판매 환급 비율

    [Header("강화 해금 (클리어한 웨이브 수 기준)")]
    public int[] unlockAfterWave = { 3, 7 };   // 1→2레벨: 3웨이브 클리어 후, 2→3레벨: 7웨이브 클리어 후

    [Header("토대 규칙")]
    public GameObject foundationPrefab;
    public int foundationCost = 30;
    public int maxFoundations = 6;
    public float foundationRefundRate = 0.5f;  // 토대 판매 환급 비율

    [Header("겉모습 위치 보정 (칸 중앙 기준)")]
    public Vector2 foundationOffset = new Vector2(0f, -0.25f);  // 토대를 아래로
    public Vector2 turretOffset = new Vector2(0f, 0.2f);        // 포탑을 위로

    [Header("맵 참조")]
    public Grid grid;
    public Tilemap groundTilemap;
    public Tilemap pathTilemap;
    public Collider2D castleCollider;
    public WaveManager waveManager;

    [Header("미리보기 (선택)")]
    public Sprite rangeCircleSprite;           // 사거리를 보여줄 원 스프라이트

    [Header("UI 연결")]
    public TMP_Text infoText;
    public Button upgradeButton;
    public TMP_Text upgradeButtonText;
    public Button sellButton;
    public TMP_Text sellButtonText;

    enum DragMode { None, Foundation, Turret }

    // 토대가 지어진 칸 → 그 위의 포탑 (null이면 빈 토대)
    readonly Dictionary<Vector3Int, Turret> foundations = new Dictionary<Vector3Int, Turret>();

    Turret selected;                    // 선택된 포탑
    Foundation selectedFoundation;      // 선택된 토대
    DragMode mode = DragMode.None;
    bool Dragging => mode != DragMode.None;

    GameObject ghost;                   // 드래그 미리보기
    SpriteRenderer ghostSr;
    GameObject ghostRange;
    GameObject rangeIndicator;          // 선택한 포탑의 사거리 원

    string message;                     // 잠깐 보여줄 안내 문구
    float messageTimer;
    int lastCleared;                    // 직전에 확인한 클리어 웨이브 수 (해금 알림용)

    void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        sellButton.onClick.AddListener(OnSellClicked);
        CreateGhost();
        CreateRangeIndicator();

        // Castle Collider 칸이 비어 있으면 태그로 자동 연결
        if (castleCollider == null)
        {
            GameObject castle = GameObject.FindWithTag("Castle");
            if (castle != null) castleCollider = castle.GetComponent<Collider2D>();
        }

        // WaveManager 칸이 비어 있으면 자동으로 찾음
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
    }

    void Update()
    {
        // 시작 전, 일시정지, 결과 화면에서는 입력을 받지 않음
        if (!GameManager.Instance.IsPlaying)
        {
            if (Dragging) EndDrag();
            return;
        }

        if (Dragging)
            UpdateDrag();
        else if (Input.GetMouseButtonDown(0))
            HandleSelectClick();

        if (messageTimer > 0) messageTimer -= Time.deltaTime;
        CheckUnlockMessage();
        RefreshUI();
    }

    // ---------- 미리보기(고스트) ----------
    void CreateGhost()
    {
        ghost = new GameObject("PlacementGhost");

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(ghost.transform, false);
        ghostSr = visual.AddComponent<SpriteRenderer>();
        ghostSr.sortingOrder = 20;

        // 사거리 원 (스프라이트를 넣었을 때만, 포탑을 끌 때만 보임)
        if (rangeCircleSprite != null)
        {
            ghostRange = new GameObject("Range");
            ghostRange.transform.SetParent(ghost.transform, false);
            SpriteRenderer rsr = ghostRange.AddComponent<SpriteRenderer>();
            rsr.sprite = rangeCircleSprite;
            rsr.sortingOrder = 19;
            rsr.color = new Color(1f, 1f, 1f, 0.2f);
            ghostRange.transform.localScale = Vector3.one * turretPrefab.range * 2f;
        }

        ghost.SetActive(false);
    }

    // 드래그 종류에 맞게 고스트 모양을 바꿈
    void SetGhostLook(DragMode m)
    {
        SpriteRenderer src = (m == DragMode.Foundation)
            ? foundationPrefab.GetComponent<SpriteRenderer>()
            : turretPrefab.GetComponent<SpriteRenderer>();

        ghostSr.sprite = src.sprite;
        ghostSr.transform.localScale = src.transform.localScale;

        if (ghostRange != null)
            ghostRange.SetActive(m == DragMode.Turret);
    }

    // ---------- 드래그 시작 (TowerDragButton이 호출) ----------
    public void BeginDragFoundation()
    {
        if (!GameManager.Instance.IsPlaying || Dragging) return;

        if (waveManager != null && waveManager.WaveRunning)
        {
            ShowMessage("토대는 웨이브 사이에만 지을 수 있습니다", 2f);
            return;
        }
        if (foundations.Count >= maxFoundations)
        {
            ShowMessage($"토대를 모두 지었습니다 (최대 {maxFoundations}개)", 2f);
            return;
        }
        if (GameManager.Instance.gold < foundationCost)
        {
            ShowMessage("골드가 부족합니다", 1.5f);
            return;
        }

        StartDrag(DragMode.Foundation);
    }

    public void BeginDragTurret()
    {
        if (!GameManager.Instance.IsPlaying || Dragging) return;

        if (!HasEmptyFoundation())
        {
            ShowMessage("빈 토대가 없습니다. 먼저 토대를 지으세요", 2f);
            return;
        }
        if (GameManager.Instance.gold < buildCost)
        {
            ShowMessage("골드가 부족합니다", 1.5f);
            return;
        }

        StartDrag(DragMode.Turret);
    }

    void StartDrag(DragMode m)
    {
        ClearSelection();
        mode = m;
        SetGhostLook(m);
        ghost.SetActive(true);
    }

    bool HasEmptyFoundation()
    {
        foreach (var pair in foundations)
            if (pair.Value == null) return true;
        return false;
    }

    // ---------- 드래그 중 ----------
    void UpdateDrag()
    {
        GetCellUnderMouse(out Vector3Int cell, out Vector3 center);

        // 고스트도 실제로 놓일 위치(보정 포함)에 보여줌
        Vector2 off = (mode == DragMode.Foundation) ? foundationOffset : turretOffset;
        ghost.transform.position = center + (Vector3)off;

        // 설치 불가 사유를 돌려받음 (null이면 설치 가능)
        string error = (mode == DragMode.Foundation)
            ? CheckFoundation(cell, center)
            : CheckTurret(cell);
        bool ok = (error == null);

        ghostSr.color = ok ? new Color(0.4f, 1f, 0.4f, 0.8f)    // 초록: 설치 가능
                           : new Color(1f, 0.4f, 0.4f, 0.8f);   // 빨강: 설치 불가

        // 우클릭으로 취소
        if (Input.GetMouseButtonDown(1))
        {
            EndDrag();
            return;
        }

        // 마우스를 떼면 설치 시도
        if (Input.GetMouseButtonUp(0))
        {
            if (!ok)
            {
                ShowMessage(error, 2f);
            }
            else if (mode == DragMode.Foundation)
            {
                if (GameManager.Instance.TrySpend(foundationCost))
                {
                    GameObject go = Instantiate(foundationPrefab,
                        center + (Vector3)foundationOffset, Quaternion.identity);

                    // 토대가 자기 칸 번호를 기억하게 함 (판매할 때 사용)
                    Foundation f = go.GetComponent<Foundation>();
                    if (f != null) f.cell = cell;

                    foundations[cell] = null;           // 빈 토대로 기록
                }
            }
            else
            {
                if (GameManager.Instance.TrySpend(buildCost))
                {
                    Turret t = Instantiate(turretPrefab,
                        center + (Vector3)turretOffset, Quaternion.identity);
                    foundations[cell] = t;              // 토대에 포탑 기록
                    SelectTurret(t);
                }
            }
            EndDrag();
        }
    }

    void EndDrag()
    {
        mode = DragMode.None;
        ghost.SetActive(false);
    }

    // ---------- 마우스 아래의 칸 계산 ----------
    void GetCellUnderMouse(out Vector3Int cell, out Vector3 center)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        cell = grid.WorldToCell(world);          // 월드 좌표 → 칸 번호
        center = grid.GetCellCenterWorld(cell);  // 그 칸의 중앙 좌표
        center.z = 0f;
    }

    // ---------- 설치 규칙 검사 (문제가 없으면 null, 있으면 사유 문구) ----------
    string CheckCommon(Vector3Int cell)
    {
        // UI 위, 맵 영역 밖
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return "맵 위에 놓아주세요";
        if (!Camera.main.pixelRect.Contains(Input.mousePosition))
            return "맵 위에 놓아주세요";
        if (!groundTilemap.HasTile(cell))
            return "맵 밖에는 설치할 수 없습니다";
        return null;
    }

    string CheckFoundation(Vector3Int cell, Vector3 center)
    {
        string common = CheckCommon(cell);
        if (common != null) return common;

        if (pathTilemap.HasTile(cell)) return "길 위에는 토대를 지을 수 없습니다";

        // 성과 한 칸이라도 겹치면 불가
        if (castleCollider != null)
        {
            Bounds cellBounds = new Bounds(center, new Vector3(grid.cellSize.x, grid.cellSize.y, 100f));
            if (castleCollider.bounds.Intersects(cellBounds)) return "성 위에는 토대를 지을 수 없습니다";
        }

        if (foundations.ContainsKey(cell)) return "이미 토대가 있습니다";
        return null;
    }

    string CheckTurret(Vector3Int cell)
    {
        string common = CheckCommon(cell);
        if (common != null) return common;

        if (!foundations.TryGetValue(cell, out Turret existing))
            return "포탑은 토대 위에만 설치할 수 있습니다";
        if (existing != null)
            return "이미 포탑이 있습니다";
        return null;
    }

    // ---------- 클릭으로 포탑/토대 선택 ----------
    void HandleSelectClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(world);

        // 1순위: 포탑
        foreach (Collider2D hit in hits)
        {
            Turret t = hit.GetComponent<Turret>();
            if (t != null) { SelectTurret(t); return; }
        }

        // 2순위: 토대
        foreach (Collider2D hit in hits)
        {
            Foundation f = hit.GetComponent<Foundation>();
            if (f == null) continue;

            // 포탑이 올라간 토대를 눌렀다면 그 포탑을 선택
            if (foundations.TryGetValue(f.cell, out Turret onIt) && onIt != null)
                SelectTurret(onIt);
            else
                SelectFoundation(f);
            return;
        }

        ClearSelection();   // 빈 땅을 누르면 선택 해제
    }

    void SelectTurret(Turret t)
    {
        if (selectedFoundation != null)
        {
            selectedFoundation.SetSelected(false);
            selectedFoundation = null;
        }
        if (selected != null) selected.SetSelected(false);
        selected = t;
        if (selected != null) selected.SetSelected(true);
        UpdateRangeIndicator();
    }

    void SelectFoundation(Foundation f)
    {
        if (selected != null)
        {
            selected.SetSelected(false);
            selected = null;
        }
        if (selectedFoundation != null) selectedFoundation.SetSelected(false);
        selectedFoundation = f;
        if (selectedFoundation != null) selectedFoundation.SetSelected(true);
        UpdateRangeIndicator();
    }

    void ClearSelection()
    {
        SelectTurret(null);   // 토대 선택도 함께 해제됨
    }

    // ---------- 사거리 원 ----------
    void CreateRangeIndicator()
    {
        if (rangeCircleSprite == null) return;

        rangeIndicator = new GameObject("RangeIndicator");
        SpriteRenderer rsr = rangeIndicator.AddComponent<SpriteRenderer>();
        rsr.sprite = rangeCircleSprite;
        rsr.sortingOrder = 4;                       // 토대(3) 위, 포탑(5) 아래
        rsr.color = new Color(1f, 1f, 1f, 0.2f);
        rangeIndicator.SetActive(false);
    }

    void UpdateRangeIndicator()
    {
        if (rangeIndicator == null) return;

        if (selected == null)
        {
            rangeIndicator.SetActive(false);
            return;
        }

        rangeIndicator.transform.position = selected.transform.position;
        rangeIndicator.transform.localScale = Vector3.one * selected.range * 2f;
        rangeIndicator.SetActive(true);
    }

    // ---------- 강화 ----------
    void OnUpgradeClicked()
    {
        if (selected == null) return;
        if (selected.level >= Turret.MaxLevel) return;

        // 웨이브 해금 조건을 만족하지 않으면 강화 불가
        if (!IsUpgradeUnlocked(selected.level))
        {
            ShowMessage($"Lv.{selected.level + 1} 강화는 {unlockAfterWave[selected.level - 1]}웨이브 클리어 후 해금됩니다", 2f);
            return;
        }

        int cost = upgradeCosts[selected.level - 1];
        if (!GameManager.Instance.TrySpend(cost)) return;   // 골드 부족 시 중단
        selected.Upgrade();
    }

    // ---------- 판매 (선택한 것이 포탑이면 포탑, 토대이면 토대) ----------
    void OnSellClicked()
    {
        if (selectedFoundation != null)
            SellFoundation();
        else if (selected != null)
            SellTurret();
    }

    void SellTurret()
    {
        // 이 포탑이 올라가 있는 토대 칸을 찾음
        Vector3Int? cell = null;
        foreach (var pair in foundations)
        {
            if (pair.Value == selected) { cell = pair.Key; break; }
        }
        if (cell == null) return;

        // 설치 비용의 50%를 돌려받음
        int refund = Mathf.RoundToInt(buildCost * sellRefundRate);
        GameManager.Instance.AddGold(refund);

        // 토대는 남기고 포탑만 제거
        foundations[cell.Value] = null;
        Turret sold = selected;
        ClearSelection();                // 선택 해제 + 사거리 원 숨김
        Destroy(sold.gameObject);

        ShowMessage($"포탑을 판매했습니다 (+{refund}골드)", 1.5f);
    }

    void SellFoundation()
    {
        // 토대는 웨이브 사이에만 판매 가능 (건설과 같은 규칙)
        if (waveManager != null && waveManager.WaveRunning)
        {
            ShowMessage("토대는 웨이브 사이에만 판매할 수 있습니다", 2f);
            return;
        }

        // 포탑이 올라가 있으면 판매 불가
        Vector3Int cell = selectedFoundation.cell;
        if (foundations.TryGetValue(cell, out Turret onIt) && onIt != null)
        {
            ShowMessage("포탑이 있는 토대는 판매할 수 없습니다", 2f);
            return;
        }

        // 건설 비용의 50%를 돌려받음
        int refund = Mathf.RoundToInt(foundationCost * foundationRefundRate);
        GameManager.Instance.AddGold(refund);

        // 칸 기록을 지우면 토대 개수가 줄고, 그 칸에 다시 지을 수 있음
        foundations.Remove(cell);
        Foundation sold = selectedFoundation;
        ClearSelection();
        Destroy(sold.gameObject);

        ShowMessage($"토대를 판매했습니다 (+{refund}골드)", 1.5f);
    }

    // ---------- 하단 패널 표시 ----------
    void RefreshUI()
    {
        int gold = GameManager.Instance.gold;
        bool showUpgrade = false;
        bool showSell = false;
        string text;

        if (mode == DragMode.Foundation)
        {
            text = "토대를 지을 풀밭에 놓으세요\n(우클릭: 취소)";
        }
        else if (mode == DragMode.Turret)
        {
            text = "빈 토대 위에 놓으세요\n(우클릭: 취소)";
        }
        else if (selectedFoundation != null)
        {
            // 빈 토대가 선택된 상태
            bool running = waveManager != null && waveManager.WaveRunning;
            int refund = Mathf.RoundToInt(foundationCost * foundationRefundRate);

            showSell = true;
            sellButton.interactable = !running;
            sellButtonText.text = $"토대 판매 +{refund}골드";
            text = running ? "빈 토대\n웨이브 중에는 판매할 수 없습니다"
                           : $"빈 토대\n판매하면 {refund}골드를 돌려받습니다";
        }
        else if (selected == null)
        {
            bool running = waveManager != null && waveManager.WaveRunning;
            string tip = running ? "웨이브 중에는 토대를 지을 수 없습니다"
                                 : "토대는 웨이브 사이에만 지을 수 있습니다";
            text = $"토대 {foundations.Count} / {maxFoundations}\n{tip}";
        }
        else
        {
            // 포탑이 선택된 상태: 판매 버튼은 항상 표시
            showSell = true;
            sellButton.interactable = true;
            int refund = Mathf.RoundToInt(buildCost * sellRefundRate);
            sellButtonText.text = $"판매 +{refund}골드";

            if (selected.level >= Turret.MaxLevel)
            {
                text = $"기본 포탑 Lv.{selected.level}\n공격력 {selected.Damage}  |  사거리 {selected.range}  |  최대 레벨";
            }
            else if (!IsUpgradeUnlocked(selected.level))
            {
                // 웨이브 해금 전: 강화 버튼은 보이지만 눌러도 잠겨 있음
                int need = unlockAfterWave[selected.level - 1];
                text = $"기본 포탑 Lv.{selected.level}\n공격력 {selected.Damage}  |  사거리 {selected.range}\nLv.{selected.level + 1} 강화는 {need}웨이브 클리어 후 해금 (현재 {ClearedWaves}웨이브 클리어)";
                showUpgrade = true;
                upgradeButton.interactable = false;
                upgradeButtonText.text = "강화 잠김";
            }
            else
            {
                int cost = upgradeCosts[selected.level - 1];
                text = $"기본 포탑 Lv.{selected.level}\n공격력 {selected.Damage} → {selected.Damage + 1}  |  사거리 {selected.range}";
                showUpgrade = true;
                upgradeButton.interactable = gold >= cost;
                upgradeButtonText.text = $"강화하기 - {cost}골드";
            }
        }

        if (messageTimer > 0) text = message;   // 안내 문구가 있으면 우선 표시

        infoText.text = text;
        upgradeButton.gameObject.SetActive(showUpgrade);
        sellButton.gameObject.SetActive(showSell);
    }

    // 클리어한 웨이브 수 (WaveManager가 없으면 0)
    int ClearedWaves => waveManager != null ? waveManager.ClearedWaves : 0;

    // 이 레벨의 포탑을 다음 레벨로 강화할 수 있는지 (웨이브 해금 조건)
    bool IsUpgradeUnlocked(int level)
    {
        int idx = level - 1;
        if (idx < 0 || idx >= unlockAfterWave.Length) return true;
        return ClearedWaves >= unlockAfterWave[idx];
    }

    // 새로 해금되는 순간에 안내 문구를 보여줌
    void CheckUnlockMessage()
    {
        int cleared = ClearedWaves;
        if (cleared == lastCleared) return;
        lastCleared = cleared;

        for (int i = 0; i < unlockAfterWave.Length; i++)
        {
            if (cleared == unlockAfterWave[i])
                ShowMessage($"{unlockAfterWave[i]}웨이브 클리어! 포탑 Lv.{i + 2} 강화가 해금되었습니다", 3f);
        }
    }

    void ShowMessage(string msg, float seconds)
    {
        message = msg;
        messageTimer = seconds;
    }
}