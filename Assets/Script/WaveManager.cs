using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 한 종류의 적을 몇 마리, 몇 초 간격으로 생성할지
[System.Serializable]
public class SpawnGroup
{
    public Enemy prefab;         // 생성할 적 프리팹
    public int count = 5;        // 마릿수
    public float interval = 1f;  // 생성 간격(초)
}

// 한 웨이브 = 체력 배율 + 여러 개의 SpawnGroup
[System.Serializable]
public class Wave
{
    public float hpScale = 1f;   // 이 웨이브 적의 체력 배율
    public SpawnGroup[] groups;
}

public class WaveManager : MonoBehaviour
{
    [Header("웨이브 구성 (10개)")]
    public Wave[] waves;

    [Header("적 등장 순서")]
    public bool shuffleOrder = true;   // 체크하면 같은 웨이브 안의 적을 무작위 순서로 생성

    [Header("UI 연결")]
    public Button nextWaveButton;

    int currentWave = 0;     // 지금까지 시작한 웨이브 수 (0이면 아직 시작 전)
    bool waveRunning;        // 웨이브가 진행 중인지

    // 다른 스크립트(HUD, BuildManager)가 읽어 가는 값 (읽기 전용)
    public int CurrentWave => currentWave;
    public int TotalWaves => waves.Length;
    public bool WaveRunning => waveRunning;

    // 클리어한 웨이브 수: 진행 중이면 현재 웨이브는 아직 클리어 전
    public int ClearedWaves => waveRunning ? currentWave - 1 : currentWave;
    
    // 해당 웨이브(1부터 시작)의 체력 배율 (HUD 표시용)
    public float HpScaleOf(int waveNumber)
    {
        if (waveNumber < 1 || waveNumber > waves.Length) return 1f;
        float s = waves[waveNumber - 1].hpScale;
        return s > 0f ? s : 1f;
    }

    void Start()
    {
        nextWaveButton.onClick.AddListener(StartNextWave);
    }

    void Update()
    {
        // 웨이브가 끝났고, 남은 웨이브가 있고, 플레이 중일 때만 버튼 활성화
        bool canStart = !waveRunning
                        && currentWave < waves.Length
                        && GameManager.Instance.IsPlaying;
        nextWaveButton.interactable = canStart;
    }

    // '다음 웨이브' 버튼을 누르면 호출됨
    void StartNextWave()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (waveRunning || currentWave >= waves.Length) return;

        currentWave++;
        StartCoroutine(RunWave(waves[currentWave - 1]));
    }

    // 지금 살아 있는 적을 직접 센다 (카운터가 어긋나도 정확함)
    int CountLivingEnemies()
    {
        int n = 0;
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (!e.IsDead) n++;
        return n;
    }

    IEnumerator RunWave(Wave wave)
    {
        waveRunning = true;
        GameManager gm = GameManager.Instance;

        // 배율이 0 이하(값이 비어 있는 경우)면 1로 취급
        float scale = wave.hpScale > 0f ? wave.hpScale : 1f;

        // 1) 이 웨이브의 적을 '한 마리씩' 목록으로 펼침
        List<(Enemy prefab, float interval)> queue = new List<(Enemy prefab, float interval)>();
        foreach (SpawnGroup g in wave.groups)
            for (int i = 0; i < g.count; i++)
                queue.Add((g.prefab, g.interval));

        // 2) 무작위 순서로 섞기
        if (shuffleOrder) Shuffle(queue);

        // 3) 전체 적 수를 '남은 적'에 먼저 더해 둠 (HUD 표시용)
        gm.aliveEnemies += queue.Count;

        // 4) 섞인 순서대로 생성
        foreach (var entry in queue)
        {
            if (gm.isGameOver)          // 도중에 게임이 끝나면 중단
            {
                waveRunning = false;
                yield break;
            }

            Enemy e = Instantiate(entry.prefab);
            e.maxHp = Mathf.Max(1, Mathf.RoundToInt(e.maxHp * scale));   // 복제본에만 체력 배율 적용

            yield return new WaitForSeconds(entry.interval);
        }

        // 5) 살아 있는 적이 하나도 없을 때까지 대기 (실제로 센 값 기준)
        while (CountLivingEnemies() > 0 && !gm.isGameOver)
            yield return null;

        gm.aliveEnemies = 0;        // 혹시 어긋났어도 웨이브가 끝난 시점에 맞춰 둠
        waveRunning = false;        // 웨이브 종료 표시
        if (gm.isGameOver) yield break;

        // 6) 마지막 웨이브까지 막았고 성 체력이 1 이상이면 클리어
        if (currentWave >= waves.Length)
            gm.Clear();
    }

    // 목록을 무작위로 섞음 (Fisher-Yates)
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);   // 0 이상 i 이하
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}