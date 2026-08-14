using System.Collections.Generic;
using Unity.VisualScripting;
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
        if(!pool.ContainsKey(prefab))
        {
            pool[prefab] = new Queue<GameObject>();
        }

        if(pool[prefab].Count > 0)
        {
            GameObject obj = pool[prefab].Dequeue();

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
        obj.SetActive(false);

        pool[prefab].Enqueue(obj);
    }
}
