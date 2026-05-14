using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 3f; // 3초 뒤 자동 삭제

    void Start()
    {
        // 총알 너무 오래 살면 렉 걸리니까 시간 지나면 자폭
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 팩트: 리지드바디 안 쓰고 그냥 트랜스폼으로 밀어도 됨 (탑다운 슈터 국룰)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 적끼리 팀킬 방지 (태그나 레이어로 막아야 함)
        if (other.CompareTag("Enemy")) return;

        // 플레이어 찾기 (인터페이스 활용)
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            Destroy(gameObject); // 명중했으니 총알 삭제
        }

        // 벽에 맞으면 삭제 (레이어 체크는 취향껏)
        else if (other.CompareTag("Wall") || other.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
}