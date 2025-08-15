// encoding: UTF-8 (65001)
using UnityEngine;

public class FireBoltSkill : MonoBehaviour, ISkill
{
    [SerializeField] private string m_SkillName = "FireBolt";
    [SerializeField] private BulletFirebolt m_BulletPrefab;

    public string SkillName => m_SkillName;

    public bool Execute(Transform owner)
    {
        if (!owner) return false;
        if (m_BulletPrefab)
        {
            var pos = owner.position;
            var dp = owner.GetComponent<IInputDirectionProvider>().GetInputDirection();
            var bolt = Pool.Spawn<BulletFirebolt>(m_BulletPrefab.gameObject, dp.pos, Quaternion.identity);
            bolt.Fire(dp.dir, owner);
        }
        Debug.Log($"[ISkill] {m_SkillName} by {owner.name}");
        return true;
    }
}
