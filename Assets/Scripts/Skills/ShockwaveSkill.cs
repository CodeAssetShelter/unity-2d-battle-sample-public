// encoding: UTF-8 (65001)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ShockwaveSkill : DamageSetter, ISkill
{
    [SerializeField] private string m_SkillName = "Shockwave";
    [SerializeField] private float m_Radius = 5f;
    [SerializeField] private LayerMask m_TargetLayers;
    [SerializeField] private LayerMask m_ExceptLayers;
    [SerializeField] private bool m_DebugDraw = true;
    [SerializeField] private ParticleSystem m_ShockwaveFx;  // PS_Shockwave 프리팹
    [SerializeField] private float m_ParticleLifetime = 0.1f;
    [SerializeField] private int m_InitialBurst = 120;

    [Header("Hemisphere / Cone Orientation")]
    [SerializeField] private Vector3 m_HemiNormal = Vector3.forward;

    public string SkillName => m_SkillName;

    public override Transform _owner { get; set; }

    private void OnEnable()
    {
        _owner = null;
    }

    public bool Execute(Transform owner)
    {
        if (!owner) return false;
        _owner = owner;

        // 중심/방향 결정
        Vector3 center = owner.position;
        Vector3 hemiN = owner.forward; // 기본: 전방 반구

        var provider = owner.GetComponent<IInputDirectionProvider>();
        if (provider != null)
        {
            var dp = provider.GetInputDirection();
            if (dp.pos != Vector3.zero) center = dp.pos;
            if (dp.dir.sqrMagnitude > 0.0001f) hemiN = dp.dir.normalized; // ★ 캐릭터가 보는 방향
        }

        Debug.Log($"[ISkill] {m_SkillName} by {owner.name} (r={m_Radius}, hemiN={hemiN})");

        // 반경 내 후보 수집 (3D)
        var hits = Physics.OverlapSphere(center, m_Radius, m_TargetLayers);

        foreach (var hit in hits)
        {
            if (!hit) continue;
            if (!IsInTargetLayer(hit.gameObject.layer)) continue;
            if (IsInExceptLayer(hit.gameObject.layer)) continue;

            Vector3 to = hit.bounds.center - center;
            float d = to.magnitude;
            if (d < Mathf.Epsilon)
            {
                OnShockwaveHit(hit.gameObject);
                continue;
            }

            Vector3 dir = to / d;
            if (Vector3.Dot(dir, hemiN) < 0f) continue; // 반대쪽 반구 제외

            float power = 1f - Mathf.Clamp01(d / Mathf.Max(0.0001f, m_Radius));
            Debug.Log($"[Shockwave Hemisphere] Hit: {hit.name} (dist={d:0.00})");
            OnShockwaveHit(hit.gameObject);
        }

#if UNITY_EDITOR
        if (m_DebugDraw)
        {
            DrawWireSphere(center, m_Radius, Color.yellow);
            Debug.DrawLine(center, center + hemiN.normalized * (m_Radius * 0.9f), Color.cyan, 0.75f);
        }
#endif

        PlayShockwaveFx(center, hemiN, m_Radius);

        return true;
    }

    private void PlayShockwaveFx(Vector3 center, Vector3 hemiN, float radius)
    {
        if (!m_ShockwaveFx) return;

        // +Z가 방사 방향인 파티클 프리팹을 hemiN 방향으로 회전
        Vector3 forward = (hemiN.sqrMagnitude > 0.0001f) ? hemiN.normalized : Vector3.forward;
        var rot = Quaternion.LookRotation(forward, Vector3.up);

        var ps = Pool.Spawn<ParticleSystem>(m_ShockwaveFx.gameObject, center, rot);
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // "반경 = 속도 × 수명"이 되도록 설정
        float life = Mathf.Max(0.05f, m_ParticleLifetime);
        main.startLifetime = life;
        main.startSpeed = radius / life;

        // Shape이 Hemisphere이며 Radius/Thickness=0으로 중앙에서만 방출되게 구성해두면 더 정확
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0f;
        shape.radiusThickness = 0f;

        // 초기 버스트
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        ps.Play(true);
        ps.Emit(m_InitialBurst);
    }

    private void OnShockwaveHit(GameObject _target)
    {
        Attack(m_Damage, _target.transform);
    }

    private bool IsInTargetLayer(int layer)
    {
        return (m_TargetLayers.value & (1 << layer)) != 0;
    }

    private bool IsInExceptLayer(int layer)
    {
        return (m_ExceptLayers.value & (1 << layer)) != 0;
    }

    private void DrawWireSphere(Vector3 c, float r, Color col, int seg = 32)
    {
        DrawWireCircle(c, Vector3.right, Vector3.up, r, col, seg); // YZ
        DrawWireCircle(c, Vector3.right, Vector3.forward, r, col, seg); // XY
        DrawWireCircle(c, Vector3.forward, Vector3.up, r, col, seg); // XZ
    }
    private void DrawWireCircle(Vector3 c, Vector3 t, Vector3 b, float r, Color col, int seg = 32)
    {
        t = t.normalized; b = b.normalized;
        float step = 2f * Mathf.PI / seg;
        Vector3 prev = c + t * r;
        for (int i = 1; i <= seg; i++)
        {
            float ang = i * step;
            Vector3 next = c + (Mathf.Cos(ang) * t + Mathf.Sin(ang) * b) * r;
            Debug.DrawLine(prev, next, col, 0.75f);
            prev = next;
        }
    }

    public override void Attack(float _damage, Transform _target)
    {
        var target = _target.GetComponentInParent<IDamageable>();
        if (target == null) return;

        target.ApplyDamage(_damage);
    }
}
