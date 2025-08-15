using UnityEngine;

public class Fire3Chain : MonoBehaviour, ISkill
{
    [SerializeField] private string m_SkillName = "3ComboChain";
    [SerializeField] private float m_Distance = 0.25f;
    [SerializeField] private BulletFireWall3Chain m_AttackPrefab;

    public string SkillName => m_SkillName;

    public bool Execute(Transform owner)
    {
        if (!owner) return false;

        var dp = owner.GetComponent<IInputDirectionProvider>().GetInputDirection();
        var dir = (dp.dir.sqrMagnitude > 0.0001f) ? dp.dir.normalized : owner.forward;

        // Owner�� �о��ִ� �͵� ���� ��������
        owner.GetComponent<Rigidbody>().AddForce(dir * m_Distance, ForceMode.Impulse);

        // ��ġ: owner ���� dir�� m_Distance ������
        var spawnPos = owner.position + dir;

        // ȸ��: dir�� �ٶ󺸰�
        var spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        var bolt = Pool.Spawn<BulletFireWall3Chain>(m_AttackPrefab.gameObject, spawnPos, spawnRot, owner);
        bolt.Fire(dp.dir, owner);

        Debug.Log($"[ISkill] {m_SkillName} by {owner.name}");
        return true;
    }
}
