using UnityEngine;

// 토대 하나가 자기 칸 번호를 기억하고, 선택 표시를 담당
public class Foundation : MonoBehaviour
{
    [HideInInspector] public Vector3Int cell;   // 이 토대가 놓인 칸 번호

    SpriteRenderer sr;
    Color baseColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    // 선택되면 살짝 노랗게 표시
    public void SetSelected(bool on)
    {
        sr.color = on ? new Color(1f, 1f, 0.6f, baseColor.a) : baseColor;
    }
}