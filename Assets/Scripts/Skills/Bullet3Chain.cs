using UnityEngine;

public class Bullet3Chain : DamageSetter
{
    public override Transform _owner { get; set; }

    private void OnEnable()
    {
        _owner = null;
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

    public void SetOwner(Transform _owner)
    {
        this._owner = _owner;
    }

    public override void Attack(float _damage, Transform _target)
    {
        var target = _target.GetComponentInParent<IDamageable>();
        if (target == null) return;

        target.ApplyDamage(_damage);
    }

    public void Release()
    {
        Pool.Despawn(this);
    }
}
