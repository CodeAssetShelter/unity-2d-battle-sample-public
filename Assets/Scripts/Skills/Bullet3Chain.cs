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

        // includeLayer�� ���Ե� ��츸 ó��
        // ��ȿ Ÿ�� ó��
        Attack(m_Damage, other.transform);
        Pool.Despawn(this);
        Debug.Log($"[Trigger] {other.name} ����");
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
