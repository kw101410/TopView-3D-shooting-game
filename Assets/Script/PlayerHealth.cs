using UnityEngine;
using System; // Action 이벤트 쓰려면 필수

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("플레이어 스탯")]
    public int maxHealth = 100;
    public int currentHealth;
    // 인터페이스 구현: 플레이어도 죽을 때 소리는 질러야지
    public event Action OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // ★ IDamageable의 핵심 함수 구현
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // 이미 죽었으면 시체훼손 금지

        currentHealth -= damage;
        Debug.Log($"[Player] 으악! 체력 남음: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("---------- GAME OVER ----------");
        currentHealth = 0;

        // 사망 이벤트 방송 (나중에 게임매니저가 이거 듣고 게임오버 창 띄움)
        OnDeath?.Invoke();

        // 플레이어는 Destroy로 지우면 카메라 꺼지고 난리 나니까 일단 비활성화만
         gameObject.SetActive(false); 

        // 혹은 조작만 끄기 (PlayerController 스크립트 있으면 끄기)
        // GetComponent<PlayerController>().enabled = false;
    }
    // 체력 비율(0.0 ~ 1.0) 알려주는 셔틀 함수
    public float GetHpRatio()
    {
        return (float)currentHealth / maxHealth;
    }
}