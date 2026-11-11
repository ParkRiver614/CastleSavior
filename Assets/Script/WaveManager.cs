using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 한 종류의 적을 몇 마리, 몇 초 간격으로 생성할지
[System.Serializable]
public class SpawnGroup
{
    public Enemy prefab;         // 생성할 적 프리팹
    public int count = 5;        // 마릿수
    public float interval = 1f;  // 생성 간격(초)
}

// 한 웨이브 = 여러 개의 SpawnGroup
[System.Serializable]
public class Wave
{
    public SpawnGroup[] groups;
}

public class WaveManager : MonoBehaviour
{
    [Header("웨이브 구성 (기획서: 총 5개)")]
    public Wave[] waves;

    [Header("UI 연결")]
    public Button nextWaveButton;

    int currentWave = 0;     // 지금까지 시작한 웨이브 수 (0이면 아직 시작 전)
    bool waveRunning;        // 웨이브가 진행 중인지

    // HUD가 읽어 가는 값 (읽기 전용)
    public int CurrentWave => currentWave;
    public int TotalWaves => waves.Length;

    void Start()
    {
        nextWaveButton.onClick.AddListener(StartNextWave);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        // 웨이브가 끝났고, 남은 웨이브가 있고, 게임이 안 끝났을 때만 버튼 활성화
        bool canStart = !waveRunning
                        && currentWave < waves.Length
                        && !gm.isGameOver
                        && !gm.isCleared;
        nextWaveButton.interactable = canStart;

    }

    // '다음 웨이브' 버튼을 누르면 호출됨
    void StartNextWave()
    {
        if (waveRunning || currentWave >= waves.Length) return;

        currentWave++;
        StartCoroutine(RunWave(waves[currentWave - 1]));
    }

    IEnumerator RunWave(Wave wave)
    {
        waveRunning = true;
        GameManager gm = GameManager.Instance;

        // 1) 이 웨이브의 전체 적 수를 먼저 '남은 적'에 더해 둠
        foreach (SpawnGroup g in wave.groups)
            gm.aliveEnemies += g.count;

        // 2) 그룹 순서대로, 정해진 간격으로 적 생성
        foreach (SpawnGroup g in wave.groups)
        {
            for (int i = 0; i < g.count; i++)
            {
                if (gm.isGameOver)          // 도중에 게임이 끝나면 중단
                {
                    waveRunning = false;
                    yield break;
                }

                Instantiate(g.prefab);      // 위치는 Enemy.Start()가 WP0로 옮겨 줌
                yield return new WaitForSeconds(g.interval);
            }
        }

        // 3) 남은 적이 모두 사라질 때까지(처치 또는 성 도착) 대기
        while (gm.aliveEnemies > 0 && !gm.isGameOver)
            yield return null;

        waveRunning = false;
        if (gm.isGameOver) yield break;

        // 4) 마지막 웨이브까지 막았고 성 체력이 1 이상이면 클리어
        if (currentWave >= waves.Length)
            gm.Clear();
    }
}
