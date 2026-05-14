using System.Collections;
using UnityEngine;

public class PlayerDashCC : MonoBehaviour
{
    private CharacterController controller;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;      // 대쉬 속도
    public float dashDuration = 0.2f;  // 얼마나 길게 미끄러질지

    private bool isDashing = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 팩트: 쉬프트 누르면 코루틴 발사
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;

        // 1. 키보드 입력
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 2. 방향 계산 (수정됨: TransformDirection 삭제)
        // 그냥 입력값 그대로 벡터를 만듦 -> 이게 월드 기준(절대 좌표)임
        Vector3 dashDir = new Vector3(h, 0, v).normalized;

        // 3. 입력이 없을 때 처리
        if (dashDir == Vector3.zero)
        {
            // 키 안 눌렀을 때:
            // 선택지 A: 마우스 보는 방향으로 나감 (공격적으로)
            dashDir = transform.forward;

            // 선택지 B: 그냥 제자리 멈춤 (방어적으로) -> 원하면 주석 해제
            // dashDir = Vector3.zero; 
        }

        // 4. 대쉬 실행
        float startTime = Time.time;

        // 팩트: 키 입력 없으면(zero) Move 실행 안 하게 예외 처리
        if (dashDir != Vector3.zero)
        {
            while (Time.time < startTime + dashDuration)
            {
                controller.Move(dashDir * dashSpeed * Time.deltaTime);
                yield return null;
            }
        }

        isDashing = false;
    }
}