// To whoever reads this. By removing the word "fuck" from one of my comments, I managed to break all my code.
// Nothing works anymore. Yet I get no errors. I hope you are happy, because I am not.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponScript : MonoBehaviour // Really proud of this script
{
    [Header("General References")]
    [SerializeField] private GenericWeaponPickup _Pickup;
    [SerializeField] private Transform _Muzzle;

    [Header("AudioVisual References")]
    [SerializeField] private AudioSource _AudioSource;
    [SerializeField] private AudioClip[] _AudioClips; // Fire, Alt Fire, Empty, Throw (discard)
    [SerializeField] private Animator _Animation; // States: Idle, Fire, Alt Fire, Empty
    [SerializeField] private float _AudioPitchMin = 0.8f;
    [SerializeField] private float _AudioPitchMax = 1.2f;

    [Header("Ammo Settings")]
    [SerializeField] private bool _AutomaticFire = false;
    [SerializeField] private float _RoundsPerMinute = 600;
    private float _LastAutomaticShot;
    [SerializeField] private string _AmmoTag;
    public int _Ammo = 30;
    [SerializeField] private bool _CanAltFire = false;
    [SerializeField] private string _AltAmmoTag;
    [SerializeField] private int _AltAmmoConsumption = 2;

    private ProjectilePool _ProjectilePool;
    private bool _CanFire; // Should be driven by animation (maybe?)
    private int _EmptyShotsUntilClogged; // Jamming system, dryfire a gun too many times and it goes manual (prevents jam sound from becoming repetitive)

    // Animation Clips
    private int _FireAnimTrigger    = Animator.StringToHash("FireTrigger");
    private int _AltAnimTrigger     = Animator.StringToHash("AltTrigger");
    private int _EmptyAnimTrigger   = Animator.StringToHash("EmptyTrigger");

    private void Awake()
    {
        // Get Pool
        _ProjectilePool = ProjectilePool.Instance;

        // Convert RPM to RPS
        _RoundsPerMinute = _RoundsPerMinute / 60;
        _EmptyShotsUntilClogged = (int) Random.Range(_RoundsPerMinute / 4, _RoundsPerMinute + 1);
    }

    void Update()
    {
        /// Controls
        if (_AutomaticFire == true)
        {
            if (Input.GetMouseButton(0)) // Primary Button (Hold)
            {
                if (Time.time - _LastAutomaticShot > 1 / _RoundsPerMinute) // Not the most stable, but the simplest solution.
                {
                    _LastAutomaticShot = Time.time;
                    if (_Ammo > 0) { FireWeapon(1, _AmmoTag, _AudioClips[0], _FireAnimTrigger); }
                    else
                    {
                        if (_EmptyShotsUntilClogged == 0) {_AutomaticFire = false;}
                        _EmptyShotsUntilClogged -= 1;
                        OutOfAmmo();
                    }
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) // Primary Button
            {
                if (_Ammo > 0) {FireWeapon(1, _AmmoTag, _AudioClips[0], _FireAnimTrigger);}
                else {OutOfAmmo();}
            }
        }

        if (!Input.GetMouseButton(0) && Input.GetMouseButtonDown(1) && _CanAltFire == true) // Secondary button
        {
            if (_Ammo > _AltAmmoConsumption - 1) {FireWeapon(_AltAmmoConsumption, _AmmoTag, _AudioClips[0], _AltAnimTrigger);}
            else {OutOfAmmo();}
        }

        if (Input.GetMouseButtonDown(2)) // Middle click
        {
            DiscardWeapon();
        }
    }

    private void FireWeapon(int AmmoConsumption, string ProjectileTag, AudioClip Sound, int AnimationID)
    {
        _Ammo -= AmmoConsumption;
        _AudioSource.PlayOneShot(Sound);
        _Animation.SetTrigger(AnimationID);

        _ProjectilePool.SpawnProjectileFromPool(ProjectileTag, _Muzzle);
        _AudioSource.pitch = (Random.Range(_AudioPitchMin, _AudioPitchMax));
    }

    private void OutOfAmmo()
    {
        _AudioSource.PlayOneShot(_AudioClips[2]);
        _AudioSource.pitch = (Random.Range(_AudioPitchMin, _AudioPitchMax));
        _Animation.SetTrigger(_EmptyAnimTrigger);
    }

    public void DiscardWeapon() // This is pretty filthy
    {
        _Pickup.gameObject.SetActive(true);
        _Pickup.DiscardWeapon();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericWeaponPickup : MonoBehaviour
{
    [Header("General References")]
    [SerializeField] private WeaponScript _WeaponScript;
    [SerializeField] private GameObject[] _Magazine; // Used to indicate if pickup has ammo, quite handy.
    [SerializeField] private Rigidbody _RigidBody;

    [Header("Settings")]
    public Vector3 _ViewModelPosition = Vector3.zero;
    public Vector3 _ViewModelRotation = Vector3.zero; // Please do not rotate the view model - this is only here because I know eventually I will do something stupid.
    [SerializeField] private float _ThrowHardness = 4f; // Does not actually do much, so long as the weapon is in motion, it stuns. So mainly just distance.

    void OnEnable()
    {
        if (_WeaponScript._Ammo == 0) {_Magazine[0].SetActive(false); _Magazine[1].SetActive(false);}
    }

    public void DiscardWeapon()
    {
        _WeaponScript.gameObject.SetActive(false);
        gameObject.transform.parent.transform.parent = null;

        _RigidBody.AddExplosionForce(125f, new Vector3(transform.position.x, transform.position.y - 0.25f, transform.position.z - 0.25f), .5f);
        _RigidBody.AddForce(transform.forward * (_ThrowHardness * 100 * _RigidBody.mass));
    }

    public void EquipWeapon()
    {
        _WeaponScript.gameObject.SetActive(true);
        gameObject.transform.position = _WeaponScript.gameObject.transform.position; // Is this necessary? - I dont know but dont touch it
        gameObject.transform.rotation = _WeaponScript.gameObject.transform.rotation;
        gameObject.SetActive(false);
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractorInventory : MonoBehaviour
{
    [Header("General References")]
    [SerializeField] private Transform _CameraTransform; // Main camera, used to tell were you looking at

    [Header("Settings")]
    [SerializeField] private float _InteractionReach = 2.5f; // How far can you grab and press stuff from?
    [SerializeField] private float _InteractionRadius = 0.35f; // Leeway

    [Header("Data")]
    [SerializeField] private int _CurrentInventorySlot = 0; // Slot goes from 0 to 2 (for a total of 3 slots, 2 main slots and 1 small one)
    [SerializeField] private GameObject[] _InventorySlots;

    private void Awake() // REMOVE THIS CODE, THERE SHOULD BE A LOCKING THINGI ON THE PLAYER MOVEMENT SCRIPT SOMEWHERE.
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(_CameraTransform.position, transform.TransformDirection(_CameraTransform.forward), out hit, _InteractionReach))
        {
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, _InteractionRadius); // haha performance go brrrrrrrrrrrrr
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.transform.tag == "Pickup")
                {
                    Debug.DrawRay(_CameraTransform.position, transform.TransformDirection(_CameraTransform.forward) * _InteractionReach, Color.green);

                    // Contextual pickup - M1 pickup only usable when no weapon is equipped.
                    if (Input.GetMouseButtonDown(0) && _InventorySlots[_CurrentInventorySlot].transform.childCount == 0 || Input.GetKeyDown(KeyCode.E)) {PickupWeapon(hitCollider.transform.gameObject);}
                }

                else {Debug.DrawRay(_CameraTransform.position, transform.TransformDirection(_CameraTransform.forward) * _InteractionReach, Color.yellow);}
            }
        }
        else
        {
            Debug.DrawRay(_CameraTransform.position, transform.TransformDirection(_CameraTransform.forward) * _InteractionReach, Color.red);
        }

        /// CONTROLS
        // Mouse Wheel - TODO: FIND A WAY SO IT DOES NOT SCROLL BY TOO FAST (Maybe adjust the >0f value?)
        if (Input.GetAxis("Mouse ScrollWheel") > 0f) // MW Up
        {
            _CurrentInventorySlot++;
            if (_CurrentInventorySlot > 2) {_CurrentInventorySlot = 0;}
            SwitchWeapon();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // MW Down
        {
            _CurrentInventorySlot--;
            if (_CurrentInventorySlot < 0) { _CurrentInventorySlot = 2;}
            SwitchWeapon();
        }

        // Keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _CurrentInventorySlot = 0;
            SwitchWeapon();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _CurrentInventorySlot = 1;
            SwitchWeapon();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _CurrentInventorySlot = 2;
            SwitchWeapon();
        }
    }

    private void PickupWeapon(GameObject Pickup) // Each time this code runs, the gun becomes 0.0002 tinier, lol.
    {
        // Set refs that will be used in the function
        Transform PickupParent = Pickup.transform.parent;
        GenericWeaponPickup PickupScript = Pickup.GetComponent<GenericWeaponPickup>();

        if (_InventorySlots[_CurrentInventorySlot].transform.childCount != 0) {_InventorySlots[_CurrentInventorySlot].GetComponentInChildren<WeaponScript>().DiscardWeapon(); } // Drop currently equipped weapon, if any
        PickupParent.transform.parent = _InventorySlots[_CurrentInventorySlot].transform;
        PickupParent.localPosition = PickupScript._ViewModelPosition; // Here lies the devils arithmetic, forever in slumber until the day it may awaken and consume my soul.
        PickupParent.localEulerAngles = PickupScript._ViewModelRotation;
        PickupScript.EquipWeapon();
    }

    private void SwitchWeapon()
    {
        foreach (GameObject Slot in _InventorySlots) {Slot.SetActive(false);}
        _InventorySlots[_CurrentInventorySlot].SetActive(true);
    }
}


using UnityEngine;

// Based on Codeers theory on how to make a projectile, it works rather well.
public class GenericProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _Damage = 25;
    [SerializeField] private float _ProjectileSpeed = 1;
    [SerializeField] private string _ImpactFX = "GenericImpact";

    private Vector3 _LastKnownPosition;
    private FXPool _FXPool;

    private void Awake() {_FXPool = FXPool.Instance;}
    private void OnEnable()
    {
        _LastKnownPosition = transform.position;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * _ProjectileSpeed * Time.deltaTime); // Uses world space. Why?
        //Debug.Log(Vector3.Distance(_LastKnownPosition, transform.position));

        RaycastHit hit;
        if (Physics.Linecast(_LastKnownPosition, transform.position, out hit))
        {
            Debug.Log("HIT! ");
            Transform OutputTransform = gameObject.transform; // Why cant I just leave it as unnasigned?
            OutputTransform.position = hit.point;
            OutputTransform.rotation = Quaternion.LookRotation(hit.normal);
            
            _FXPool.SpawnFXFromPool(_ImpactFX, OutputTransform); // You can use Vector3.Reflect to bounce the projectile right back lol
            gameObject.SetActive(false);
        }
        Debug.DrawLine(_LastKnownPosition, transform.position, Color.cyan);
    }

    private void LateUpdate() {_LastKnownPosition = transform.position;}
}
