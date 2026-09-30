using System.Collections.Generic;
using UnityEngine;

public static class SimplePool
{
    private static Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private static Dictionary<GameObject, GameObject> instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

    // Instantiate の代わりに使う
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        GameObject obj;

        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Object.Instantiate(prefab, position, rotation);
            instanceToPrefabMap[obj] = prefab;
        }

        return obj;
    }

    // Destroy の代わりに使う
    public static void Despawn(GameObject obj)
    {
        if (instanceToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            obj.SetActive(false);
            poolDictionary[prefab].Enqueue(obj);
        }
        else
        {
            Object.Destroy(obj);
        }
    }
}