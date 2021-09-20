using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// PLACE ON INIT SCENE.
public class ProjectilePool : MonoBehaviour // Thanks Brackeys
{
    public static ProjectilePool Instance;

    [System.Serializable]
    public class Pool
    {
        public string _PoolTag;
        public GameObject _PoolPrefab;
        public int _PoolMaxSize;
    }

    public List<Pool> _PoolList;
    public Dictionary<string, Queue<GameObject>> _PoolDictionary;
    private void Awake()
    {
        Instance = this;
        _PoolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach(Pool ActivePool in _PoolList)
        {
            Queue<GameObject> ObjectPool = new Queue<GameObject>();

            for (int i = 0; i < ActivePool._PoolMaxSize; i++)
            {
                GameObject GeneratedPoolObject = Instantiate(ActivePool._PoolPrefab, gameObject.transform);
                GeneratedPoolObject.SetActive(false);
                ObjectPool.Enqueue(GeneratedPoolObject);
            }

            _PoolDictionary.Add(ActivePool._PoolTag, ObjectPool);
        }
    }

    public void SpawnProjectileFromPool(string tag, Transform Location)
    {
        GameObject ProjectileToSpawn = _PoolDictionary[tag].Dequeue(); // Reminder: Projectile Scripts should use OnEnable() rather than Awake or Start.
        _PoolDictionary[tag].Enqueue(ProjectileToSpawn);

        ProjectileToSpawn.transform.position = Location.transform.position;
        ProjectileToSpawn.transform.rotation = Location.transform.rotation;
        ProjectileToSpawn.SetActive(true);
    }

    /* === This is how you call the function above ===
     * Put this on awake: _ProjectilePool = ProjectilePool.Instance; // Make sure to have private ProjectilePool _ProjectilePool;
     * Then this on your shooting function: _ProjectilePool.SpawnProjectileFromPool(ProjectileTag, _Muzzle);
    */
}
