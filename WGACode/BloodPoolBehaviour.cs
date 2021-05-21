using UnityEngine;
using DG.Tweening;

public class BloodPoolBehaviour : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _Sprite;
    [SerializeField] private Color[] _Colors; // Rot, Transparent

    private void Awake()
    {
        Invoke("InitiateExpansion", Random.Range(0.25f, 2.5f));
    }

    private void InitiateExpansion()
    {
        float finalsize = Random.Range(0.25f, 1.15f);
        float timeuntilfullsize = Random.Range(3f, 12f);

        // Begin to grow pool
        gameObject.transform.DOScale(new Vector3(finalsize, finalsize, finalsize), timeuntilfullsize);
        Invoke("BeginRotting", timeuntilfullsize + Random.Range(2f, 10f));
    }

    private void BeginRotting()
    {
        float timeuntilrottingends = Random.Range(12f, 300f);

        _Sprite.DOColor(_Colors[0], timeuntilrottingends);
        Invoke("CleanUpBlood", timeuntilrottingends);
    }

    private void CleanUpBlood()
    {
        float timeuntilfullygone = Random.Range(6f, 30f);

        _Sprite.DOColor(_Colors[1], timeuntilfullygone);
        Destroy(gameObject, timeuntilfullygone);
    }
}
