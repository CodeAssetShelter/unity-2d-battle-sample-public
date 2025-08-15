using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public interface IInputDirectionProvider
{
    (Vector3 dir, Vector3 pos) GetInputDirection();
}

public interface ILockPlayer
{
    public void LockPlayer(bool _isOn);
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, IInputDirectionProvider, ILockPlayer
{
    [SerializeField] private Transform m_Pivot;
    [SerializeField] private bool m_IsPlayer = true;
    [SerializeField] private bool m_Lock = false;

    [Header("Move / Jump Config")]
    [SerializeField] private float m_MoveSpeed = 6f;
    [SerializeField] private float m_JumpHeight = 1.6f;
    [SerializeField] private Vector3 m_LastDirection = Vector3.right;
    [SerializeField] private HeadLocator m_HL;

    [Header("Ground Check (3D)")]
    [SerializeField] private LayerMask m_GroundLayers = 1 << 0;
    [SerializeField] private Vector3 m_GroundCheckOffset = new Vector3(0f, -0.9f, 0f);
    [SerializeField] private float m_GroundCheckRadius = 0.18f;
    [SerializeField] private float m_GroundCheckDistance = 0.12f;

    [Header("Fall / Jump tuning")]
    [SerializeField] private float m_FallMultiplier = 2.5f;
    [SerializeField] private float m_LowJumpMultiplier = 2.0f;

    [Header("Rotation / Physics Safety")]
    [SerializeField] private bool m_EnableAirYawFreeze = true;    // 공중일 때 Y 회전 잠금 사용 여부
    [SerializeField] private bool m_ZeroAngularVelocityY = true;  // 공중일 때 Y 각속도(angularVelocity.y) 강제 0 적용 여부
    [SerializeField] private float m_AngularDragWhileAir = 2f;    // 공중일 때 임시 angularDrag (0이면 변경 안함)

    [Header("Debug")]
    [SerializeField] private bool m_EnableDebug = false;

    // 내부
    private Rigidbody m_Rb;
    private InputAction m_MoveAction;
    private InputAction m_JumpAction;
    private Vector2 m_MoveInput = Vector2.zero;

    private bool m_JumpRequested = false;
    private bool m_JumpHeld = false;

    private RigidbodyConstraints m_OriginalConstraints;
    private float m_OriginalAngularDrag;

    private void Awake()
    {
        BindPlayer();

        m_Rb = GetComponent<Rigidbody>();
        if (m_Rb == null)
        {
            Debug.LogError("[PlayerMovement] Rigidbody 컴포넌트 없음. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        // 원래 제약 및 angularDrag 저장
        m_OriginalConstraints = m_Rb.constraints;
        m_OriginalAngularDrag = m_Rb.angularDamping;

        // 기본으로 X/Z 축 회전 고정(디자이너가 원하면 인스펙터에서 변경)
        if ((m_OriginalConstraints & RigidbodyConstraints.FreezeRotationX) == 0 &&
            (m_OriginalConstraints & RigidbodyConstraints.FreezeRotationZ) == 0)
        {
            // 기존값을 덮어쓰지 않기 위해 OR 방식으로 설정
            m_Rb.constraints = m_OriginalConstraints | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            m_OriginalConstraints = m_Rb.constraints; // 업데이트
        }

        if (m_IsPlayer)
            CreateInputActions();

        if (m_EnableDebug)
            Debug.Log($"[PlayerMovement][Awake] originalConstraints={m_OriginalConstraints}, originalAngularDrag={m_OriginalAngularDrag}");
    }

    private void OnEnable()
    {
        if (m_MoveAction != null) m_MoveAction.Enable();

        if (m_JumpAction != null)
        {
            m_JumpAction.Enable();
            m_JumpAction.started += OnJumpStarted;
            m_JumpAction.performed += OnJumpPerformed;
            m_JumpAction.canceled += OnJumpCanceled;
        }
    }

    private void OnDisable()
    {
        if (m_JumpAction != null)
        {
            m_JumpAction.started -= OnJumpStarted;
            m_JumpAction.performed -= OnJumpPerformed;
            m_JumpAction.canceled -= OnJumpCanceled;
            m_JumpAction.Disable();
        }

        if (m_MoveAction != null) m_MoveAction.Disable();

        // 종료 시 물리 설정 복원
        if (m_Rb != null)
        {
            m_Rb.constraints = m_OriginalConstraints;
            m_Rb.angularDamping = m_OriginalAngularDrag;
        }
    }

    private void CreateInputActions()
    {
        m_MoveAction = new InputAction("Move", InputActionType.Value);
        m_MoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        m_MoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        m_MoveAction.AddBinding("<Gamepad>/leftStick");

        m_JumpAction = new InputAction("Jump", InputActionType.Button);
        m_JumpAction.AddBinding("<Keyboard>/space");
        m_JumpAction.AddBinding("<Keyboard>/y");
    }

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        m_JumpHeld = true;
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        m_JumpRequested = true;
        if (m_EnableDebug) Debug.Log("[PlayerMovement] Jump requested");
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        m_JumpHeld = false;
    }

    private void Update()
    {
        if (m_MoveAction != null && m_IsPlayer) m_MoveInput = m_MoveAction.ReadValue<Vector2>();
    }

    public void SetMoveInputAxis(Vector2 _dir)
    {
        m_MoveInput = _dir;
    }

    [SerializeField] private Transform m_Target;      // 추적 대상
    [SerializeField] private string m_PlayerTag = "Player";

    private Vector2 m_CurrentDir = Vector2.zero;      // 스무딩 현재 방향
    private Coroutine m_CoChaseMode = null;

    /// <summary>
    /// 외부에서 호출하여 추적 모드 시작
    /// </summary>
    public void ActiveChaseMode(float _speed, float _chaseTime, float _followSharpness)
    {
        BindPlayer();
        if (m_Target == null) return;

        // 기존 코루틴 중지 후 새로 시작
        if (m_CoChaseMode != null)
            StopCoroutine(m_CoChaseMode);

        m_CoChaseMode = StartCoroutine(m_CorChaseMode(_speed, _chaseTime, _followSharpness));
    }

    private void BindPlayer()
    {
        // 타겟 없으면 태그로 탐색
        if (m_Target == null)
        {
            var go = GameObject.FindGameObjectWithTag(m_PlayerTag);
            if (go != null) m_Target = go.transform;
        }
    }

    /// <summary>
    /// 실제 추적 코루틴
    /// </summary>
    private IEnumerator m_CorChaseMode(float _speed, float _chaseTime, float _followSharpness)
    {
        m_CurrentDir = Vector2.zero;
        SetMoveInputAxis(Vector2.zero);

        float elapsed = 0f;

        while (elapsed < _chaseTime)
        {
            if (m_Target == null) break;

            // 타겟 방향 계산 (XZ 평면 기준)
            Vector3 selfPos = transform.position;
            Vector3 targetPos = m_Target.position;
            Vector3 toTarget = targetPos - selfPos;

            Vector2 desiredDir = new Vector2(toTarget.x, toTarget.z);
            if (desiredDir.sqrMagnitude > 0.0001f)
                desiredDir.Normalize();
            else
                desiredDir = Vector2.zero;

            // 스무딩
            float t = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);
            m_CurrentDir = Vector2.Lerp(m_CurrentDir, desiredDir, t);

            // 이동 입력 반영
            SetMoveInputAxis(m_CurrentDir * _speed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 종료 시 입력 초기화
        SetMoveInputAxis(Vector2.zero);
        m_CoChaseMode = null;
    }

    private void FixedUpdate()
    {
        if (m_Lock) return;
        ApplyMovement();

        if (m_JumpRequested)
        {
            if (IsGrounded())
            {
                DoJump();
                if (m_EnableDebug) Debug.Log("[PlayerMovement] Jump executed");
            }
            else
            {
                if (m_EnableDebug) Debug.Log("[PlayerMovement] Jump ignored - not grounded");
            }
            m_JumpRequested = false;
        }

        ApplyVariableGravity();

        // 공중 Y 회전 제어
        try
        {
            bool grounded = IsGrounded();

            if (m_EnableAirYawFreeze && !grounded)
            {
                // 공중일 때 Y 회전 잠금 FreezeRotationY 추가
                m_Rb.constraints = m_OriginalConstraints | RigidbodyConstraints.FreezeRotationY;

                // 공중 angularDrag 보정
                if (m_AngularDragWhileAir > 0f) m_Rb.angularDamping = m_AngularDragWhileAir;

                // Y 각속도 강제
                if (m_ZeroAngularVelocityY)
                {
                    var av = m_Rb.angularVelocity;
                    av.y = 0f;
                    m_Rb.angularVelocity = av;
                }

                if (m_EnableDebug)
                {
                    Debug.Log($"[PlayerMovement] Air yaw frozen. angularVel={m_Rb.angularVelocity}");
                }
            }
            else
            {
                // 지면 복귀 시 원래 제약/drag 복원
                m_Rb.constraints = m_OriginalConstraints;
                if (m_Rb.angularDamping != m_OriginalAngularDrag) m_Rb.angularDamping = m_OriginalAngularDrag;
            }
        }
        catch (Exception ex)
        {
            if (m_EnableDebug) Debug.LogError($"[PlayerMovement] Rotation safety logic exception: {ex}");
        }
    }

    private void ApplyMovement()
    {
        if (m_Rb == null) return;
        if (m_MoveInput != Vector2.zero)
        {
            m_LastDirection.x = m_MoveInput.x;
            m_LastDirection.y = 0;
            m_LastDirection.z = m_MoveInput.y;
        }
        m_HL?.SetAxis(m_MoveInput);
        Vector3 desired = new Vector3(m_MoveInput.x * m_MoveSpeed, m_Rb.linearVelocity.y, m_MoveInput.y * m_MoveSpeed);
        m_Rb.linearVelocity = desired;
    }

    private void DoJump()
    {
        if (m_Rb == null) return;
        float g = Mathf.Abs(Physics.gravity.y);
        float v = Mathf.Sqrt(2f * g * Mathf.Max(0f, m_JumpHeight));
        Vector3 vel = m_Rb.linearVelocity;
        vel.y = v;
        m_Rb.linearVelocity = vel;
        m_JumpHeld = true;
    }

    private void ApplyVariableGravity()
    {
        if (m_Rb == null) return;

        float dt = Time.fixedDeltaTime;
        Vector3 vel = m_Rb.linearVelocity;

        if (vel.y < 0f)
        {
            vel += Vector3.up * Physics.gravity.y * (m_FallMultiplier - 1f) * dt;
        }
        else if (vel.y > 0f && !m_JumpHeld)
        {
            vel += Vector3.up * Physics.gravity.y * (m_LowJumpMultiplier - 1f) * dt;
        }

        m_Rb.linearVelocity = vel;
    }

    private bool IsGrounded()
    {
        if (transform == null) return false;
        Vector3 origin = transform.position + m_GroundCheckOffset;

        // OverlapSphere로 즉시 접촉 확인
        Collider[] overlaps = Physics.OverlapSphere(origin, m_GroundCheckRadius, m_GroundLayers, QueryTriggerInteraction.Ignore);
        if (overlaps != null && overlaps.Length > 0)
        {
            if (m_EnableDebug)
            {
                foreach (var c in overlaps)
                {
                    if (c != null) Debug.Log($"[IsGrounded] Overlap hit: {c.name} (layer {c.gameObject.layer})");
                }
            }
            return true;
        }

        // 조금 위에서 Raycast
        float rayDist = Mathf.Max(0.02f, m_GroundCheckDistance);
        Ray ray = new Ray(origin + Vector3.up * 0.05f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit rh, rayDist + 0.05f, m_GroundLayers, QueryTriggerInteraction.Ignore))
        {
            if (m_EnableDebug) Debug.Log($"[IsGrounded] Raycast hit: {rh.collider.name} (dist {rh.distance})");
            return true;
        }

        return false;
    }


    public (Vector3 dir, Vector3 pos) GetInputDirection()
    {
        Vector3 dir = m_LastDirection;

        if (!m_IsPlayer)
        {
            dir = m_Target.transform.position - transform.position;
            dir.y = 0;
            dir = dir.normalized;
            // Sign 처리하면 근사치를 따라가는 형태가 됨
        }

        Vector3 pos = m_Pivot == null ? transform.position : m_Pivot.position;
        return (dir.normalized, pos);
    }
    public void LockPlayer(bool _isOn)
    {
        m_Lock = _isOn;
        if (m_Lock)
        {
            m_MoveInput = Vector2.zero;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 o = (transform != null ? transform.position : Vector3.zero) + m_GroundCheckOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(o, m_GroundCheckRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o + Vector3.up * 0.05f, o + Vector3.down * (m_GroundCheckDistance + 0.05f));
    }


#endif
}
