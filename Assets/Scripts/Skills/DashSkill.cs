using UnityEngine;

public class DashSkill : MonoBehaviour, ISkill
{
    [SerializeField] private string m_SkillName = "Dash";
    [SerializeField] private float m_Distance = 4f;

    public string SkillName => m_SkillName;

    public bool Execute(Transform owner)
    {
        if (!owner) return false;
        var dp = owner.GetComponent<IInputDirectionProvider>().GetInputDirection();
        owner.GetComponent<Rigidbody>().AddForce(dp.dir * m_Distance, ForceMode.Impulse);
        Debug.Log($"[ISkill] {m_SkillName} by {owner.name}");
        return true;
    }
}
