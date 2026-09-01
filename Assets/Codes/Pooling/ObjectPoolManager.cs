using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();

    public GameObject Get(GameObject prefab)
    {
        // 프리팹이 없거나(참조 파괴/미할당) 하면 안전하게 무시.
        if (prefab == null)
        {
            return null;
        }

        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        if (pool[prefab].Count > 0)
        {
            GameObject obj = pool[prefab].Dequeue();

            if (obj == null)
            {
                return Instantiate(prefab);
            }

            obj.SetActive(true);

            return obj;
        }
        else
        {
            return Instantiate(prefab);
        }
    }

    public void Return(GameObject prefab, GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        obj.SetActive(false);

        if (prefab == null)
        {
            return;
        }

        if (!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        pool[prefab].Enqueue(obj);
    }
}
