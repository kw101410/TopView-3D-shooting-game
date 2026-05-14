using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerHealth playerHealth;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        // 구독: 체력 스크립트가 "나 죽었어(OnDeath)" 외치면 -> OnPlayerDied 실행
        if (playerHealth != null)
        {
            playerHealth.OnDeath += OnPlayerDied;
        }
    }

    void OnDisable()
    {
        // 구독 취소: 메모리 누수 방지 (습관 들여라)
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= OnPlayerDied;
        }
    }

    void OnPlayerDied()
    {
        // 아까 만든 트리거 당기기
        animator.SetTrigger("doDie");
    }
}