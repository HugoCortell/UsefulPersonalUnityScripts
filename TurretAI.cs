using UnityEngine;
using UnityEngine.SceneManagement;

public class TurretAI : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private bool _IsDebugEnabled = false;
    [SerializeField] private AbilityInvoker _VoiceRecog;
    [SerializeField] private GameObject _PlayerCollider; // Because this is the (true) player pos (Change it to camera if it does not look right)

    [Header("Behavioural Settings")]
    [SerializeField] private float _SightCheckRate = 0.15f;
    [SerializeField] private int _FireThreshold = 15;

    [SerializeField] private float _PushForceUp = 1f;
    [SerializeField] private float _PushBackForce = 8f;

    [Header("Model Renderer Settings")]
    [SerializeField] private GameObject _TurretModel;

    [Header("Laser Settings")]
    [SerializeField] private GameObject _LaserOBJ;

    [SerializeField] private LineRenderer _LaserRenderer;

    [Header("AudioVisual Settings")]
    [SerializeField] private GameObject _Light;
    [SerializeField] private ParticleSystem _MuzzleFlash;
    [SerializeField] private AudioSource _AudioSource;
    [SerializeField] private AudioClip[] _Aclips; // 0 FIRE // 1 - 5 WARNING SHOT (VA) // 6 - 8 LAST WARNING (VA)
    [SerializeField] private float _InitialLaserLength = 5f;

    [Header("Automated Data - DO NOT MODIFY")]
    public bool _IsPlayerVisible = false;
    public int _RepeatedHits = 0; // I really wish it were not but hey, its a hotfix.
    [SerializeField] private int _LocalPlayerHealth = 3; // Each gun tracks health independently, this makes the game easier. (Like, a lot easier lol)

    private void Start() // Should I use Awake instead?
    {
        // Line Renderer Adjustments
        _LaserRenderer.material = new Material(Shader.Find("Sprites/Default")); // Makes Gradient Colours Work
        _LaserRenderer.SetPosition(0, _LaserOBJ.transform.position);
        _LaserRenderer.SetPosition(1, _LaserOBJ.transform.position + _LaserOBJ.transform.forward * _InitialLaserLength); // It randomly decided to work on its own lol

        // Cheap-out Update
        InvokeRepeating("CheckSight", 1f, _SightCheckRate);
    }

    private void CheckSight()
    {
        if (_IsPlayerVisible == true)
        {
            _TurretModel.transform.LookAt(_PlayerCollider.transform.position);
            _LaserRenderer.SetPosition(0, _LaserOBJ.transform.position);

            RaycastHit hit;
            if (Physics.Raycast(_LaserOBJ.transform.position, (_PlayerCollider.transform.position - _LaserOBJ.transform.position) + new Vector3(0, _PlayerCollider.GetComponent<CapsuleCollider>().center.y, 0), out hit, Mathf.Infinity, ~(1 << 2)))
            {
                if (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 11) {_RepeatedHits++;} // Layer 8 is player
                else {_RepeatedHits = 0;}
            }

            if (_IsDebugEnabled == true){Debug.DrawRay(_LaserOBJ.transform.position, (_PlayerCollider.transform.position - _LaserOBJ.transform.position) + new Vector3(0, _PlayerCollider.GetComponent<CapsuleCollider>().center.y, 0), Color.red);}
            _LaserRenderer.SetPosition(1, hit.point); // Makes laser sight end at hit position

            // If player has been in sight for X amount of hits.
            if (_RepeatedHits >= _FireThreshold)
            {
                if (_IsDebugEnabled == true){Debug.Log("<color=red>TURRET DEBUG:</color> SHOT FIRED!");}

                // AudioVisual Stuffs
                _AudioSource.PlayOneShot(_Aclips[0]); // Assign the clip to the source - for now.
                _Light.SetActive(true); Invoke("DisableLight", 0.065f); // Wow thats a low number (ikr)
                _MuzzleFlash.Play(); // Lasts about 0.05f


                // Shield-Related stuff
                if (hit.transform.gameObject.name == "Shield") {hit.transform.gameObject.GetComponent<ShieldManager>().BreakShield();}
                // Player pushback w/ race case issue mitigation (slower but oh well, im not an engi)
                else if (hit.transform.gameObject.name == "CharacterController") {hit.transform.gameObject.GetComponent<PlayerRigidbodyRedirector>()._Rigidbody.AddExplosionForce(_PushBackForce,  transform.position, Mathf.Infinity, _PushForceUp, ForceMode.Impulse); _LocalPlayerHealth--;}
                else {hit.transform.gameObject.GetComponent<Rigidbody>().AddExplosionForce(_PushBackForce,  transform.position, Mathf.Infinity, _PushForceUp, ForceMode.Impulse); _LocalPlayerHealth--;}
                

                // Health Dependant Actions - USE AN EXTERNAL (2D) AUDIO SOURCE FOR THIS OR MAKE IT COMMUNICATE WITH THE PLAYER UPON HIT OR SOMETHING
                //if (_LocalPlayerHealth > 1) {_AudioSource.PlayOneShot(_Aclips[Random.Range (1, 5], 1f);}
                //else if (_LocalPlayerHealth == 1) {_AudioSource.PlayOneShot(_Aclips[Random.Range (6, 8], 1f);}
                if (_LocalPlayerHealth <= 0) {Invoke("KillPlayer", .5f);} // MAKE IT AN ELSE IF LIKE THE OTHERS!!
                _RepeatedHits = -(_FireThreshold/3); // After being shot, the threshold goes into negative 1/3rd of threshold to allow the player some extra time. I think this is good design.
            }
        }
    }

    private void KillPlayer()
    {
        if (_IsDebugEnabled == true){Debug.Log("<color=red>TURRET DEBUG:</color> Player was shot to death!");}

        _VoiceRecog.OnApplicationQuit();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Animation Specials
    private void DisableLight() {_Light.SetActive(false);}
}
