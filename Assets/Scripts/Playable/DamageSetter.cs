using UnityEngine;

public abstract class DamageSetter : MonoBehaviour
{
    public float m_Damage;
    public abstract Transform _owner { get; set; }

    public abstract void Attack(float _damage, Transform _target);
    // ���߿� ���� �߰� ���� �ʿ��ϸ� �ۼ�
}
