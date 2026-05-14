using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class Wave
{
    public int count = 10;
    public float spawnInterval = 1.0f;
}

public class WaveManager : MonoBehaviour
{
    // ★ 다른 스크립트에서 얘 상태를 봐야 하니까 싱글톤으로 만듦
    public static WaveManager Instance;

    [Header("Wave Settings")]
    public List<Wave> waves;
    public Transform[] spawnPoints;
    public float timeBetweenWaves = 5f; // 쇼핑해야 하니까 시간 좀 넉넉하게 5초?

    [Header("UI Settings")]
    public GameObject waveNoticePanel;
    public TMP_Text waveNoticeText;
    public GameObject victoryPanel;

    [Header("Shop Settings (추가됨)")]
    public GameObject shopHintText; // "B 눌러 상점 열기" 텍스트

    [Header("Pool Settings")]
    public GameObject zombiePrefab;

    // ★ 핵심: 지금이 휴식 시간인가? (외부에서 읽기 가능)
    public bool isBreakTime = false;

    private ObjectPool<GameObject> enemyPool;
    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;
    private bool isSpawning = false;

    void Awake()
    {
        if (Instance == null) Instance = this; // 싱글톤 초기화

        enemyPool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject enemy = Instantiate(zombiePrefab);
                var ai = enemy.GetComponent<ZombieAI>();
                if (ai != null) ai.SetPool(enemyPool);
                return enemy;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            maxSize: 500
        );
    }

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        StartCoroutine(StartNextWave(currentWaveIndex));
    }

    IEnumerator StartNextWave(int index)
    {
        if (index >= waves.Count)
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0;
            yield break;
        }

        // --- 휴식 시간 시작 ---
        isBreakTime = true; // ★ 쇼핑 가능!
        if (shopHintText != null) shopHintText.SetActive(true); // "B 누르세요" 켜기
        if (waveNoticePanel != null) waveNoticePanel.SetActive(true);

        float timeLeft = timeBetweenWaves;
        while (timeLeft > 0)
        {
            if (waveNoticeText != null)
                waveNoticeText.text = $"Wave {index + 1}\nStart in {Mathf.Ceil(timeLeft)}...";

            yield return null;
            timeLeft -= Time.deltaTime;
        }

        // --- 휴식 시간 끝 (전투 시작) ---
        isBreakTime = false; // ★ 쇼핑 금지!
        if (shopHintText != null) shopHintText.SetActive(false); // 힌트 끄기

        // ★ 중요: 혹시 상점 열어놓고 멍때리다가 웨이브 시작되면 상점 강제로 닫아야 함
        if (GameManager.Instance != null) GameManager.Instance.ForceCloseShop();

        if (waveNoticeText != null) waveNoticeText.text = "Wave Start!";
        yield return new WaitForSeconds(1.0f);

        if (waveNoticePanel != null) waveNoticePanel.SetActive(false);
        StartCoroutine(SpawnWave(waves[index]));
    }

    IEnumerator SpawnWave(Wave _wave)
    {
        isSpawning = true;
        for (int i = 0; i < _wave.count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(_wave.spawnInterval);
        }
        isSpawning = false;
        currentWaveIndex++;
    }

    void SpawnEnemy()
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = enemyPool.Get();

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(sp.position);
            enemy.transform.rotation = sp.rotation;
        }
        else
        {
            enemy.transform.position = sp.position;
            enemy.transform.rotation = sp.rotation;
        }

        enemiesAlive++;

        IDamageable damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.OnDeath -= OnEnemyKilled;
            damageable.OnDeath += OnEnemyKilled;
        }
    }

    public void OnEnemyKilled()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0 && !isSpawning)
        {
            StartCoroutine(WaveClearRoutine());
        }
    }

    IEnumerator WaveClearRoutine()
    {
        if (waveNoticePanel != null && waveNoticeText != null)
        {
            waveNoticeText.text = $"Wave {currentWaveIndex} Cleared!";
            waveNoticePanel.SetActive(true);
        }

        yield return new WaitForSeconds(2.0f);
        // 2초 뒤 StartNextWave 호출되면서 다시 휴식 시간(쇼핑 가능) 됨
        if (waveNoticePanel != null) waveNoticePanel.SetActive(false);
        StartCoroutine(StartNextWave(currentWaveIndex));
    }
}