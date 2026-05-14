using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool; // 풀링 필수
using System.Collections;
using System; // Action 필수

// 1. 상태 인터페이스
public interface IZombieState
{
    void Enter();
    void Execute();
    void Exit();
}

// 2. 좀비(적) 본체
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour, IDamageable
{
    [Header("전투 설정")]
    public int zombieDamage = 10;

    [Header("스탯 설정")]
    public float maxHp = 30f;
    public float attackRange = 8.0f; // 팩트: 총 쏘니까 사거리 길어야 함
    public float turnSpeed = 5.0f;   // 회전 속도

    [Header("원거리 공격 설정")]
    public GameObject bulletPrefab;  // 총알 프리팹
    public Transform firePoint;      // 총구 위치 (빈 오브젝트)

    // 내부 변수
    private float currentHp;
    private IObjectPool<GameObject> myPool;
    private bool isDead;

    // 컴포넌트
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform target;

    // 상태 머신
    private IZombieState currentState;
    public IZombieState ChaseState { get; private set; }
    public IZombieState AttackState { get; private set; }
    public IZombieState DeadState { get; private set; }

    // ★ IDamageable 구현: 죽음 이벤트
    public event Action OnDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 상태 생성
        ChaseState = new ZombieChaseState(this);
        AttackState = new ZombieAttackState(this);
        DeadState = new ZombieDeadState(this);
    }

    private void OnEnable()
    {
        // 기존 초기화 코드들...
        currentHp = maxHp;
        isDead = false;
        GetComponent<Collider>().enabled = true;

        // ★ 중요: Agent 켜기 전에 데이터 초기화
        if (agent != null)
        {
            agent.enabled = true; // 일단 켬
            agent.ResetPath();    // 기존 경로 삭제 (찌꺼기 제거)
        }

        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;

        // ★ 핵심 수정: 바로 ChangeState 하지 말고, 코루틴으로 한 프레임 뜀
        // ChangeState(ChaseState); <--- 이거 지우고 아래 걸로 바꿔
        StartCoroutine(StartChaseRoutine());
    }

    // 한 프레임 쉬고 추적 시작하는 코루틴
    IEnumerator StartChaseRoutine()
    {
        // 1프레임 대기 (Warp가 적용되고 NavMesh에 안착할 시간 벌기)
        yield return null;

        // 혹시 그 사이에 죽었거나 꺼졌으면 무시
        if (!gameObject.activeSelf || isDead) yield break;

        // 안전장치: 진짜 NavMesh 위에 있나?
        if (agent.isOnNavMesh)
        {
            ChangeState(ChaseState);
        }
        else
        {
            // 만약 1프레임 쉬었는데도 NavMesh 위에 없으면?
            // 강제로 가장 가까운 NavMesh 찾아서 Warp 시도 (최후의 수단)
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                ChangeState(ChaseState);
            }
            else
            {
                Debug.LogError($"[ZombieAI] 야, 나({gameObject.name}) 스폰됐는데 바닥(NavMesh)이 없다. 좌표: {transform.position}");
                // 에러 나도 게임 안 터지게 일단 끔
                gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (currentState != null) currentState.Execute();
    }

    public void ChangeState(IZombieState newState)
    {
        if (currentState == newState) return;
        if (currentState != null) currentState.Exit();

        currentState = newState;
        currentState.Enter();
    }

    // 풀링 매니저가 호출
    public void SetPool(IObjectPool<GameObject> pool)
    {
        myPool = pool;
    }

    // --- 전투 로직 ---

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;
        // animator.SetTrigger("Hit"); // 피격 모션 있으면 주석 해제

        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(100);
        }
        OnDeath?.Invoke(); // 매니저한테 사망 신고
        ChangeState(DeadState);
    }

    public void StartDeathSequence()
    {
        StartCoroutine(ReturnToPoolAfterDelay(2f));
    }

    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (myPool != null) myPool.Release(gameObject);
        else Destroy(gameObject);
    }

    // ★ 애니메이션 이벤트에서 호출 (Attack 모션의 특정 프레임에 Event 추가해야 함)
    public void OnAttackHit()
    {
        if (isDead || target == null) return;

        if (bulletPrefab != null && firePoint != null)
        {
            // 1. 총알을 생성하고 변수에 담는다
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, transform.rotation);

            // 2. 총알 스크립트를 가져온다
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();

            // 3. ★핵심★ 데미지를 주입한다
            if (bulletScript != null)
            {
                bulletScript.SetDamage(zombieDamage);
            }
        }
        else
        {
            Debug.LogError("[ZombieAI] 총알 프리팹이나 FirePoint가 없습니다!");
        }
    }
}

// --- 상태 클래스 ---

// 1. 추적 상태 (사거리 밖일 때)
public class ZombieChaseState : IZombieState
{
    private ZombieAI zombie;
    private float pathUpdateTimer;

    public ZombieChaseState(ZombieAI zombie) => this.zombie = zombie;

    public void Enter()
    {
        zombie.animator.SetBool("IsChase", true);
        zombie.agent.isStopped = false;
    }

    public void Execute()
    {
        if (zombie.target == null) return;

        // 거리 체크 (제곱 거리 사용으로 최적화)
        float distSqr = (zombie.target.position - zombie.transform.position).sqrMagnitude;

        // 사거리 안에 들어오면 공격 상태로 전환
        if (distSqr <= zombie.attackRange * zombie.attackRange)
        {
            zombie.ChangeState(zombie.AttackState);
            return;
        }

        // 경로 갱신 (0.2초마다)
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= 0.2f)
        {
            zombie.agent.SetDestination(zombie.target.position);
            pathUpdateTimer = 0f;
        }
    }

    public void Exit()
    {
        zombie.animator.SetBool("IsChase", false);
        zombie.agent.isStopped = true; // 멈춤
    }
}

// 2. 공격 상태 (사거리 안일 때)
// 2. 공격 상태 (사거리 안일 때) - 캡슐용 타이머 버전
public class ZombieAttackState : IZombieState
{
    private ZombieAI zombie;
    private float attackTimer; // 공격 쿨타임 재는 시계

    public ZombieAttackState(ZombieAI zombie) => this.zombie = zombie;

    public void Enter()
    {
        zombie.animator.SetBool("IsAttack", true);
        zombie.agent.isStopped = true; // 이동 정지

        // 상태 들어오자마자 바로 쏠지, 기다렸다 쏠지 결정
        // 여기선 0으로 해서 들어오자마자 쿨타임 돌게 함 (즉발 사격 원하면 attackTimer = 100f 하셈)
        attackTimer = 0f;
    }

    public void Execute()
    {
        if (zombie.target == null)
        {
            zombie.ChangeState(zombie.ChaseState);
            return;
        }

        // 1. 거리 체크: 플레이어가 도망가서 사거리 벗어나면 다시 추적
        float distSqr = (zombie.target.position - zombie.transform.position).sqrMagnitude;
        if (distSqr > (zombie.attackRange + 1.0f) * (zombie.attackRange + 1.0f))
        {
            zombie.ChangeState(zombie.ChaseState);
            return;
        }

        // 2. 회전: 플레이어 바라보기 (Y축만 회전)
        Vector3 targetPos = zombie.target.position;
        targetPos.y = zombie.transform.position.y;

        Vector3 dir = (targetPos - zombie.transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            zombie.transform.rotation = Quaternion.Slerp(zombie.transform.rotation, lookRot, Time.deltaTime * zombie.turnSpeed);
        }

        // ★★★ [수정됨] 캡슐이라 애니메이션 이벤트가 없으니 코드로 시간 잼 ★★★
        attackTimer += Time.deltaTime;

        // 좀비 설정에 있는 쿨타임(attackCooldown)보다 시간이 지났으면 발사
        // (참고: ZombieAI 위쪽에 public float attackCooldown 변수 있다고 가정함. 없으면 2.0f 라고 적으셈)
        if (attackTimer >= 2.0f) // 쿨타임 2초라고 가정 (변수 있으면 zombie.attackCooldown 쓰셈)
        {
            zombie.OnAttackHit(); // 강제 발사 명령!
            attackTimer = 0f;     // 시계 초기화
        }
    }

    public void Exit()
    {
        zombie.animator.SetBool("IsAttack", false);
    }
}

// 3. 사망 상태
public class ZombieDeadState : IZombieState
{
    private ZombieAI zombie;
    public ZombieDeadState(ZombieAI zombie) => this.zombie = zombie;

    public void Enter()
    {
        zombie.animator.SetTrigger("Death");
        zombie.agent.isStopped = true;
        zombie.GetComponent<Collider>().enabled = false; // 시체 충돌 끔
        zombie.StartDeathSequence();
    }

    public void Execute() { }
    public void Exit() { }
}