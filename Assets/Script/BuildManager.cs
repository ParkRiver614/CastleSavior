using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class BuildManager : MonoBehaviour
{
    [Header("규칙 (기획서 수치)")]
    public Turret turretPrefab;
    public int buildCost = 60;
    public int[] upgradeCosts = { 80, 120 };   // 1→2레벨, 2→3레벨

    [Header("맵 참조")]
    public Grid grid;
    public Tilemap groundTilemap;
    public Tilemap pathTilemap;
    public Collider2D castleCollider;

    [Header("미리보기 (선택)")]
    public Sprite rangeCircleSprite;           // 사거리를 보여줄 원 스프라이트

    [Header("UI 연결")]
    public TMP_Text infoText;
    public Button upgradeButton;
    public TMP_Text upgradeButtonText;

    // 이미 포탑이 놓인 칸 목록 (겹침 방지)
    readonly HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();

    Turret selected;          // 선택된 포탑
    bool dragging;            // 드래그 중인지
    GameObject ghost;         // 미리보기 오브젝트
    SpriteRenderer ghostSr;

    string message;           // 잠깐 보여줄 안내 문구
    float messageTimer;

    void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        CreateGhost();
    }

    void Update()
    {
        if (dragging)
            UpdateDrag();
        else if (Input.GetMouseButtonDown(0))
            HandleSelectClick();

        if (messageTimer > 0) messageTimer -= Time.deltaTime;
        RefreshUI();
    }

    // ---------- 미리보기(고스트) ----------
    void CreateGhost()
    {
        ghost = new GameObject("PlacementGhost");
        ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = turretPrefab.GetComponent<SpriteRenderer>().sprite;
        ghostSr.sortingOrder = 20;

        // 사거리 원 (스프라이트를 넣었을 때만)
        if (rangeCircleSprite != null)
        {
            GameObject r = new GameObject("Range");
            r.transform.SetParent(ghost.transform, false);
            SpriteRenderer rsr = r.AddComponent<SpriteRenderer>();
            rsr.sprite = rangeCircleSprite;
            rsr.sortingOrder = 19;
            rsr.color = new Color(1f, 1f, 1f, 0.2f);
            r.transform.localScale = Vector3.one * turretPrefab.range * 2f;
        }

        ghost.SetActive(false);
    }

    // ---------- 드래그 시작 (TowerDragButton이 호출) ----------
    public void BeginDrag()
    {
        if (dragging) return;

        if (GameManager.Instance.gold < buildCost)
        {
            ShowMessage("골드가 부족합니다", 1.5f);
            return;
        }

        Select(null);
        dragging = true;
        ghost.SetActive(true);
    }

    // ---------- 드래그 중 ----------
    void UpdateDrag()
    {
        GetCellUnderMouse(out Vector3Int cell, out Vector3 center);
        ghost.transform.position = center;

        bool ok = CanPlace(cell, center);
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
            if (ok)
            {
                if (GameManager.Instance.TrySpend(buildCost))
                {
                    Turret t = Instantiate(turretPrefab, center, Quaternion.identity);
                    occupied.Add(cell);
                    Select(t);
                }
            }
            else
            {
                ShowMessage("여기에는 설치할 수 없습니다", 1.5f);
            }
            EndDrag();
        }
    }

    void EndDrag()
    {
        dragging = false;
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

    // ---------- 설치 가능 여부 (기획서 규칙) ----------
    bool CanPlace(Vector3Int cell, Vector3 center)
    {
        // UI 위에서는 설치 불가
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (!groundTilemap.HasTile(cell)) return false;   // 맵 밖
        if (pathTilemap.HasTile(cell)) return false;      // 길 위
        if (occupied.Contains(cell)) return false;        // 이미 포탑이 있음
        if (castleCollider != null && castleCollider.OverlapPoint(center)) return false;  // 성 위

        return true;
    }

    // ---------- 설치된 포탑 선택 ----------
    void HandleSelectClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Turret found = null;
        foreach (Collider2D hit in Physics2D.OverlapPointAll(world))
        {
            found = hit.GetComponent<Turret>();
            if (found != null) break;
        }
        Select(found);   // 빈 땅을 누르면 null → 선택 해제
    }

    void Select(Turret t)
    {
        if (selected != null) selected.SetSelected(false);
        selected = t;
        if (selected != null) selected.SetSelected(true);
    }

    // ---------- 강화 ----------
    void OnUpgradeClicked()
    {
        if (selected == null) return;
        if (selected.level >= Turret.MaxLevel) return;

        int cost = upgradeCosts[selected.level - 1];
        if (!GameManager.Instance.TrySpend(cost)) return;   // 골드 부족 시 중단
        selected.Upgrade();
    }

    // ---------- 하단 패널 표시 ----------
    void RefreshUI()
    {
        int gold = GameManager.Instance.gold;
        bool showUpgrade = false;
        string text;

        if (dragging)
        {
            text = "원하는 위치에 놓으세요\n(우클릭: 취소)";
        }
        else if (selected == null)
        {
            text = $"포탑 버튼({buildCost}골드)을 맵으로 드래그해서 설치하세요";
        }
        else if (selected.level >= Turret.MaxLevel)
        {
            text = $"기본 포탑 Lv.{selected.level}\n공격력 {selected.Damage}  |  최대 레벨";
        }
        else
        {
            int cost = upgradeCosts[selected.level - 1];
            text = $"기본 포탑 Lv.{selected.level}\n공격력 {selected.Damage}  →  {selected.Damage + 1}";
            showUpgrade = true;
            upgradeButton.interactable = gold >= cost;
            upgradeButtonText.text = $"강화하기 - {cost}골드";
        }

        if (messageTimer > 0) text = message;   // 안내 문구가 있으면 우선 표시

        infoText.text = text;
        upgradeButton.gameObject.SetActive(showUpgrade);
    }

    void ShowMessage(string msg, float seconds)
    {
        message = msg;
        messageTimer = seconds;
    }
}