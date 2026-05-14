using System; // Action 쓰려면 필요

public interface IDamageable
{
    void TakeDamage(int damage);

    // 팩트: 인터페이스에 이벤트를 선언해서 모든 적이 "죽음 알림"을 보장하게 함
    event Action OnDeath;
}