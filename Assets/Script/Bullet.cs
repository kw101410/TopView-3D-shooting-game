using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    private IObjectPool<Bullet> _pool;

    [Header("기본 속도 설정")]
    [SerializeField] private float moveSpeed = 50f;

    [Header("타격 이펙트 설정 (2개 다 나옴)")]
    [SerializeField] private GameObject hitEffect1; // 첫 번째 이펙트 (예: 스파크)
    [SerializeField] private GameObject hitEffect2; // 두 번째 이펙트 (예: 연기/피)

    [Header("충돌 설정")]
    [SerializeField] private LayerMask hitLayer;

    private TrailRenderer trail;
    private float _defaultSpeed;
    private int currentDamage;

    public void SetDamage(int dmg)
    {
        currentDamage = dmg;
    }

    private void Awake()
    {
        _defaultSpeed = moveSpeed;
        trail = GetComponent<TrailRenderer>();
    }

    // Gun.cs 같은 데서 풀 연결할 때 호출
    public void SetPool(IObjectPool<Bullet> pool) => _pool = pool;

    private void OnEnable()
    {
        moveSpeed = _defaultSpeed;

        // 트레일 초기화 (잔상 남는 버그 방지)
        if (trail != null) trail.Clear();

        // 3초 뒤 자동 반납 (허공에 쐈을 때)
        Invoke(nameof(ReturnToPool), 3f);
    }

    private void Update()
    {
        float moveDistance = moveSpeed * Time.deltaTime;

        // 레이캐스트로 미리 충돌 감지 (빠른 총알 터널링 방지)
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance, hitLayer))
        {
            HandleHit(hit);
        }
        else
        {
            transform.Translate(Vector3.forward * moveDistance);
        }
    }

    private void HandleHit(RaycastHit hit)
    {
        // 1. 이펙트 1번 생성
        if (hitEffect1 != null)
        {
            GameObject effect = Instantiate(hitEffect1, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
        }

        // 2. 이펙트 2번 생성 (동시 출력)
        if (hitEffect2 != null)
        {
            GameObject effect = Instantiate(hitEffect2, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
        }

        IDamageable target = hit.collider.GetComponent<IDamageable>();
        if (target == null) target = hit.collider.GetComponentInParent<IDamageable>();

        if (target != null)
        {
            // ★ 여기가 핵심 변경점
            target.TakeDamage(currentDamage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        CancelInvoke();
        if (_pool != null) _pool.Release(this);
        else Destroy(gameObject); // 풀 연결 안 됐으면 그냥 삭제
    }
}