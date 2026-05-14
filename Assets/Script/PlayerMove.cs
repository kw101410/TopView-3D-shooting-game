using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private LayerMask groundLayer; // 바닥 Layer (Ground)

    // ★ 추가됨: 애니메이터 제어용 변수
    // 인스펙터에서 플레이어 캐릭터(Animator 있는 놈) 드래그해서 넣으셈
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private InputAction moveAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // 컨트롤러 켜져 있는지 확인 (너를 못 믿어서 넣음 ㅋㅋ)
        if (!controller.enabled)
        {
            Debug.LogError("🚨 야! CharacterController 체크박스 꺼져있잖아! 당장 켜!");
            controller.enabled = true; // 에라 모르겠다 강제로 켜주마
        }

        // ★ 추가됨: 혹시 인스펙터에서 깜빡하고 안 넣었으면, 자식 오브젝트 뒤져서라도 찾아옴
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        var playerInput = new InputActionMap("Player");
        moveAction = playerInput.AddAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.Enable();
    }

    private void Update()
    {
        // 1. 이동 입력 받기
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);

        // ★★★ 여기가 애니메이션 핵심 ★★★
        // 입력값(Vector2)의 길이가 0보다 크면 "키보드 누르고 있다"는 뜻임.
        // sqrMagnitude가 0.01보다 크면 움직이는 걸로 간주 (루트 연산 안 해서 빠름)
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (animator != null)
        {
            // 애니메이터 파라미터 "IsRun"을 켜거나 끔
            // 뛰면 true, 멈추면 false. 아주 직관적이지?
            animator.SetBool("isRun", isMoving);
        }

        controller.Move(move * Time.deltaTime * moveSpeed);

        // 2. 중력 (공중부양 방지)
        if (controller.isGrounded && playerVelocity.y < 0) playerVelocity.y = 0f;
        playerVelocity.y += -9.81f * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // 3. 회전 (마우스 바라보기)
        // Mouse.current가 null일 수도 있으니 안전장치 추가함
        if (Mouse.current != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                Vector3 targetPosition = hit.point;
                targetPosition.y = transform.position.y;
                transform.LookAt(targetPosition);

                // 시선 잘 따라가는지 확인용 빨간 선
                Debug.DrawLine(transform.position, hit.point, Color.red);
            }
        }
    }
}