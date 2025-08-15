// UTF-8 (Code page 65001)
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}

public static class Pool
{
    // ---- Static storages ----
    private static readonly Dictionary<int, PrefabPoolBase> m_Pools = new Dictionary<int, PrefabPoolBase>(); // key: prefab.GetInstanceID()
    private static readonly Dictionary<int, PrefabPoolBase> m_InstanceIdToPool = new Dictionary<int, PrefabPoolBase>(); // key: instance GameObject ID
    private static Transform m_Root;

    // ---------- Public API ----------
    public static void Prewarm<T>(GameObject _prefab, int _count, Transform _container = null) where T : Component
    {
        var pool = GetOrCreatePool<T>(_prefab, _container);
        pool.Prewarm(_count);
    }

    public static T Spawn<T>(GameObject _prefab, Vector3 _pos, Quaternion _rot, Transform _parent = null) where T : Component
    {
        var pool = GetOrCreatePool<T>(_prefab, null);
        var comp = (T)pool.Spawn(_pos, _rot, _parent);
        return comp;
    }

    public static void Despawn(Component _instance)
    {
        if (_instance == null) return;
        int id = _instance.gameObject.GetInstanceID();
        if (m_InstanceIdToPool.TryGetValue(id, out var pool))
        {
            pool.DespawnByComponent(_instance);
        }
        else
        {
            // 풀 소속이 아닌 경우: 안전하게 비활성화 or Destroy
            _instance.gameObject.SetActive(false);
        }
    }

    // ---------- Internals ----------
    private static PrefabPoolBase GetOrCreatePool<T>(GameObject _prefab, Transform _container) where T : Component
    {
        if (_prefab == null)
        {
            Debug.LogError("Pool: Prefab is null.");
            return null;
        }

        int key = _prefab.GetInstanceID();
        if (m_Pools.TryGetValue(key, out var existing))
        {
            // 타입 검증
            if (!existing.IsType(typeof(T)))
            {
                Debug.LogError($"Pool: Requested type {typeof(T).Name} does not match existing pool type {existing.GetItemType().Name} for prefab '{_prefab.name}'.");
            }
            return existing;
        }

        EnsureRoot();
        var pool = new PrefabPool<T>(_prefab, _container ? _container : m_Root, RegisterInstanceOwner, UnregisterInstanceOwner);
        m_Pools.Add(key, pool);
        return pool;
    }

    private static void EnsureRoot()
    {
        if (m_Root != null) return;
        var rootGO = new GameObject("[PoolRoot]");
        m_Root = rootGO.transform;
        Object.DontDestroyOnLoad(rootGO);
    }

    private static void RegisterInstanceOwner(GameObject _instanceGO, PrefabPoolBase _owner)
    {
        if (_instanceGO == null) return;
        m_InstanceIdToPool[_instanceGO.GetInstanceID()] = _owner;
    }

    private static void UnregisterInstanceOwner(GameObject _instanceGO)
    {
        if (_instanceGO == null) return;
        m_InstanceIdToPool.Remove(_instanceGO.GetInstanceID());
    }

    // ========= Base / Generic Pool =========
    public abstract class PrefabPoolBase
    {
        public readonly GameObject Prefab;
        protected readonly Transform m_Container;

        protected PrefabPoolBase(GameObject _prefab, Transform _container)
        {
            Prefab = _prefab;
            m_Container = _container;
        }

        public abstract Component Spawn(Vector3 _pos, Quaternion _rot, Transform _parent);
        public abstract void DespawnByComponent(Component _comp);
        public abstract void Prewarm(int _count);

        public abstract System.Type GetItemType();
        public bool IsType(System.Type _t) => GetItemType() == _t;
    }

    public sealed class PrefabPool<T> : PrefabPoolBase where T : Component
    {
        private readonly Stack<T> m_Free = new Stack<T>(64);
        private readonly List<T> m_All = new List<T>(64);
        private readonly System.Action<GameObject, PrefabPoolBase> m_RegisterOwner;
        private readonly System.Action<GameObject> m_UnregisterOwner;

        public PrefabPool(GameObject _prefab, Transform _container,
                          System.Action<GameObject, PrefabPoolBase> _reg,
                          System.Action<GameObject> _unreg)
            : base(_prefab, _container)
        {
            m_RegisterOwner = _reg;
            m_UnregisterOwner = _unreg;
        }

        public override Component Spawn(Vector3 _pos, Quaternion _rot, Transform _parent)
        {
            T item;
            if (m_Free.Count > 0)
            {
                item = m_Free.Pop();
            }
            else
            {
                var go = Object.Instantiate(Prefab, m_Container);
                go.SetActive(false); // 생성 직후에는 비활성화 상태로
                if (!go.TryGetComponent<T>(out item))
                {
                    // 강제 붙여야 한다면 다음 줄을 사용(권장X): item = go.AddComponent<T>();
                    Debug.LogError($"Pool<{typeof(T).Name}>: Prefab '{Prefab.name}' does not have required component.");
                    item = go.AddComponent<T>(); // 최소한의 복구
                }
                m_All.Add(item);
                m_RegisterOwner?.Invoke(go, this);
            }

            var tr = item.transform;
            if (_parent) tr.SetParent(_parent, false);
            tr.SetPositionAndRotation(_pos, _rot);

            item.gameObject.SetActive(true);
            if (item is IPoolable p) p.OnSpawned();
            return item;
        }

        public override void DespawnByComponent(Component _comp)
        {
            if (_comp == null) return;
            if (!(_comp is T item)) item = _comp.GetComponent<T>();
            if (item == null) return;

            if (item is IPoolable p) p.OnDespawned();

            var tr = item.transform;
            tr.SetParent(m_Container, false);
            item.gameObject.SetActive(false);
            m_Free.Push(item);
        }

        public override void Prewarm(int _count)
        {
            for (int i = 0; i < _count; i++)
            {
                var go = Object.Instantiate(Prefab, m_Container);
                go.SetActive(false);
                if (!go.TryGetComponent<T>(out var item))
                {
                    Debug.LogError($"Pool<{typeof(T).Name}>: Prefab '{Prefab.name}' does not have required component.");
                    item = go.AddComponent<T>();
                }
                m_All.Add(item);
                m_Free.Push(item);
                m_RegisterOwner?.Invoke(go, this);
            }
        }

        public override System.Type GetItemType() => typeof(T);
    }
}
