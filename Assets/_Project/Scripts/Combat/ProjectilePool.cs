using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    private static ProjectilePool instance;
    public static ProjectilePool Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("ProjectilePool");
                instance = go.AddComponent<ProjectilePool>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private Dictionary<GameObject, Queue<GameObject>> pools = new();
    private Dictionary<GameObject, int> poolSizes = new();
    
    [SerializeField] private int defaultPoolSize = 20;
    [SerializeField] private bool autoExpand = true;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    public void PrewarmPool(GameObject prefab, int size)
    {
        if (pools.ContainsKey(prefab)) return;
        
        CreatePool(prefab, size);
    }
    
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab, defaultPoolSize);
        }
        
        var pool = pools[prefab];
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else if (autoExpand)
        {
            obj = CreatePooledObject(prefab);
        }
        else
        {
            Debug.LogWarning($"Pool for {prefab.name} is empty and auto-expand is disabled!");
            return null;
        }
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        
        return obj;
    }
    
    public void Return(GameObject obj, GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"No pool exists for {prefab.name}");
            Destroy(obj);
            return;
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pools[prefab].Enqueue(obj);
    }
    
    private void CreatePool(GameObject prefab, int size)
    {
        var pool = new Queue<GameObject>();
        pools[prefab] = pool;
        poolSizes[prefab] = size;
        
        for (int i = 0; i < size; i++)
        {
            var obj = CreatePooledObject(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    private GameObject CreatePooledObject(GameObject prefab)
    {
        var obj = Instantiate(prefab);
        obj.transform.SetParent(transform);
        
        // Add return component
        var poolable = obj.AddComponent<PoolableObject>();
        poolable.prefab = prefab;
        
        return obj;
    }
}

public class PoolableObject : MonoBehaviour
{
    public GameObject prefab;
    
    public void ReturnToPool()
    {
        ProjectilePool.Instance.Return(gameObject, prefab);
    }
}