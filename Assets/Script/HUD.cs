using UnityEngine;
using TMPro;

// 상단 바: 성 체력, 골드, 웨이브, 남은 적을 화면에 표시
public class HUD : MonoBehaviour
{
    [Header("표시할 글자")]
    public TMP_Text hpText;
    public TMP_Text goldText;
    public TMP_Text waveText;
    public TMP_Text enemyText;

    [Header("참조")]
    public WaveManager waveManager;

    void Update()
    {
        GameManager gm = GameManager.Instance;

        // 성 체력: 남은 체력만큼 하트를 표시
        hpText.text = $"<size=55%>성 체력</size>\n<color=#FF6B7A>{new string('♥', gm.castleHp)}</color>";

        // 보유 골드
        goldText.text = $"<size=55%>보유 골드</size>\n<color=#FFD54F>◆ {gm.gold}</color>";

        // 웨이브 (시작 전에는 1/5로 표시)
        int shown = Mathf.Max(1, waveManager.CurrentWave);
        waveText.text = $"<size=55%>웨이브</size>\n{shown} / {waveManager.TotalWaves}";

        // 남은 적
        enemyText.text = $"<size=55%>남은 적</size>\n{Mathf.Max(0, gm.aliveEnemies)}";
    }
}