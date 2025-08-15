using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CharacterStatus : MonoBehaviour, IDamageable
{
    [SerializeField] protected float m_MaxHP = 100;
    [SerializeField] protected float m_HP = 100;

    public bool ApplyDamage(float _damage)
    {
        bool isDead = false;
        m_HP = Mathf.Clamp(m_HP - _damage, 0, 100);
        isDead = m_HP <= 0;

        UIManager.Instance.UpdateHP(m_HP / m_MaxHP, gameObject.CompareTag("Player"));

        // ������ ��Ʈ ����Ʈ�� �ϳ��ϱ� ���⿡
        // ���Ŀ� �� ���� ��ü�� ����
        // Body�� 0.5 �� �̹Ƿ� �׽�Ʈ ���� �ϵ��ڵ�
        Pool.Spawn<ParticleSystem>(Resources.Load<GameObject>("HitEfx"), transform.position + (Vector3.up * 0.5f), Quaternion.identity, null);

        if (isDead)
            Dead();
        return isDead;
    }

    private void Dead()
    {
        GameManager.Instance.NaJugoSSoyo(transform);
    }
}
