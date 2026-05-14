using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;

    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private int maxEnemies = 100;

    // ★ 중요: CountActive 쓰려면 인터페이스(IObjectPool) 말고 구체 클래스(ObjectPool) 써야 함
    private ObjectPool<GameObject> enemyPool;
    private float timer;

    private void Awake()
    {
        // 풀 초기화
        enemyPool = new ObjectPool<GameObject>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            maxSize: maxEnemies
        );
    }

    private void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;

        // 시간 됐고 + 풀 공간 남았으면 스폰
        if (timer >= spawnInterval && enemyPool.CountActive < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        // 1. 플레이어 주변 원형 범위에서 랜덤 위치 잡기
        Vector2 randomPoint = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = new Vector3(player.position.x + randomPoint.x, player.position.y, player.position.z + randomPoint.y);

        // 2. NavMesh 위인지 확인 (허공 방지)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 5.0f, NavMesh.AllAreas))
        {
            GameObject enemy = enemyPool.Get();
            enemy.transform.position = hit.position;
            enemy.transform.rotation = Quaternion.LookRotation(player.position - hit.position);
        }
    }

    // --- 풀링 이벤트 함수들 ---
    private GameObject CreateEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab);
        // ★ 핵심: 좀비한테 "니네 집(풀)은 여기야"라고 알려줌
        // 좀비 프리팹에 ZombieAI 컴포넌트 반드시 있어야 함
        enemy.GetComponent<ZombieAI>().SetPool(enemyPool);
        return enemy;
    }

    private void OnGetEnemy(GameObject enemy) => enemy.SetActive(true);
    private void OnReleaseEnemy(GameObject enemy) => enemy.SetActive(false);
    private void OnDestroyEnemy(GameObject enemy) => Destroy(enemy);
}