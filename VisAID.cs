using UnityEngine;

public class VisAID : MonoBehaviour
{
    [Header("Basics")]
    [SerializeField] private bool _EnableVisAID = true;
    [SerializeField] private Color _Colour = new Color(1f, 0f, 0f, 1f);

    [Header("Sphere Rendering")]
    [SerializeField] private bool _EnableCentralSphere = true;
    [SerializeField] private float _SphereSize = .15f;

    [Header("Line Rendering")]
    [SerializeField] private bool _EnableLineRendering = false;
    [SerializeField] private float _LineLength = 1f;
    [SerializeField] private bool _EnableSphereEndPoint = false;

    private void OnDrawGizmosSelected() 
    {
        if (_EnableVisAID)
        {
            Gizmos.color = _Colour;
            if (_EnableCentralSphere)
            {
                Gizmos.DrawWireSphere(transform.position, _SphereSize);
            }

            if (_EnableLineRendering)
            {
                Gizmos.DrawLine(transform.position, transform.position + transform.right * _LineLength);
                if (_EnableSphereEndPoint) {Gizmos.DrawWireSphere(transform.position + transform.right * _LineLength, _SphereSize);}
            }
        }
    }
}
