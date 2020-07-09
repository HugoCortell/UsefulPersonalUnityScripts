using UnityEngine;

// THIS IS ALL LEGACY CODE AND ITS BROKEN AS SHIT PLEASE DONT USE IT.
public class KillLaser : MonoBehaviour
{
    // Some of thse options are actually for the subchild, but are declared here for ease of management. Im lazy.
    [Header("Raycast Settings")]
    [SerializeField] private bool _IsRaycastAutomatic = false; // This can lag and cause some strange behaviour - best keep disabled if possible.
    
    [Header("Damage Settings")]

    [Header("Graphics Settings Settings")]
    [SerializeField] private GameObject _LaserVisual;
    public Material _LaserVisualMat;

    // Recommended settings: DEFAULT
    [SerializeField] private float _XMultiplier = 0.2f;
    [SerializeField] private float _YMultiplier = 0.5f;

    [SerializeField] private bool _IsAlbedoEnabled = false;
    [SerializeField] private bool _IsNormalEnabled = false;
    [SerializeField] private bool _IsNoiseEnabled = true;

    [Header("Extras")]
    [SerializeField] private bool _IsDebugEnabled;
    public KillLaserTrigger _ChildScript;
    protected float _RayLength = 1;

    void Start()
    {
        // Reference set up
        _LaserVisualMat = _LaserVisual.GetComponent<Renderer>().material; // Assign material to reduce searches.
        _ChildScript = _LaserVisual.GetComponent<KillLaserTrigger>(); // Assigns child subscript that handles trigger-based interactions.
        _ChildScript._ParentScript = this;
        
        // Refresh system selection
        if (_IsRaycastAutomatic == true) {InvokeRepeating("RecalculateDistance", 1.0f, 0.25f);}
        else {RecalculateDistance();}
    }

    private void RecalculateDistance()
    {
        RaycastHit hit;
        Ray LaserRaycast = new Ray(transform.position, Vector3.forward);
        if (_IsDebugEnabled == true){Debug.DrawRay(transform.position, transform.forward * _RayLength, Color.yellow);}

        if (Physics.Raycast(LaserRaycast, out hit))
        {
            _RayLength = hit.distance; // Store raycast distance
            if (_IsDebugEnabled == true){Debug.Log("<color=red>DEBUG:</color> LASER RAYCAST RECALCULATED: NEW LENGTH IS " + _RayLength);}

            // Adjust scale to hit distance, because its on an anchor it will only adjust one edge.
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, _RayLength);
            _LaserVisual.GetComponent<Light>().range = _RayLength;
            
            // AUTO-UV GENERATION
            if (_IsAlbedoEnabled == true){_LaserVisualMat.SetTextureScale("_MainTex", new Vector2(_RayLength / _XMultiplier, _YMultiplier));}
            if (_IsNormalEnabled == true){_LaserVisualMat.SetTextureScale("_Normal", new Vector2(_RayLength / _XMultiplier, _YMultiplier));}
            if (_IsNoiseEnabled == true){_LaserVisualMat.SetTextureScale("_NoiseTex", new Vector2(_RayLength / _XMultiplier, _YMultiplier));}
        }
    }
}
