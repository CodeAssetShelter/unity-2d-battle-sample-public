using UnityEngine;

public class BulletFirebolt : DamageSetter
{
    [SerializeField] private Rigidbody m_Rb;
    [SerializeField] private float m_Reach = 5f;
    [SerializeField] private float m_DisableTime = 0.1f;
    [SerializeField] private SphereCollider m_Collider;
    [SerializeField] private float m_Pow = 2.5f;
    [SerializeField] private Vector3 m_Dir = Vector3.right;
    

    private Vector3 _startVec;
    private float _elapsed = 0;
    private bool _disappearMode = false;
    public override Transform _owner { get; set; }

    public float _Dis = 0;

    public void OnEnable()
    {
        _owner = null;
        _elapsed = 0;
        _disappearMode = false;
        m_Collider.enabled = true;
        transform.localEulerAngles = Vector3.one;
        transform.localScale = Vector3.one;
        _startVec = transform.position;
        m_Rb.linearVelocity = Vector3.zero;
        //Fire(m_Dir);
    }

    public void Fire(Vector3 _dir, Transform _owner)
    {
        this._owner = _owner;
        m_Rb.AddForce(_dir * m_Pow, ForceMode.Impulse);
    }

    public void Update()
    {
        _Dis = Vector3.Distance(transform.position, _startVec);
        if (!_disappearMode && Vector3.Distance(transform.position, _startVec) > m_Reach)
        {
            _disappearMode = true;
            m_Collider.enabled = false;
        }
        if (_disappearMode && _elapsed < m_DisableTime)
        {
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, _elapsed / m_DisableTime);
            _elapsed += Time.deltaTime;

            if (_elapsed > m_DisableTime)
            {
                Pool.Despawn(this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_owner && (other.transform == _owner || other.transform.IsChildOf(_owner)))
            return;

        // includeLayer에 포함된 경우만 처리
        // 유효 타격 처리
        Attack(m_Damage, other.transform);
        Pool.Despawn(this);
        Debug.Log($"[Trigger] {other.name} 진입");
    }

    public override void Attack(float _damage, Transform _target)
    {
        var target = _target.GetComponentInParent<IDamageable>();
        if (target == null) return;

        target.ApplyDamage(_damage);
    }
}
