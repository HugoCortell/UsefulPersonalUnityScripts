using UnityEngine;
using SensorToolkit;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class AICore : MonoBehaviour
{
    [Header("Player Refs")]
    [SerializeField] public GameObject _PlayerTorso;
    [SerializeField] private Rigidbody _PlayerRB;

    [Header("Agent Refs")]
    [SerializeField] private GameObject _Agent;
    [SerializeField] private NavMeshAgent _Pathfinder;
    [SerializeField] private float[] _AgentSpeeds = {2.5f, 5f};
    [SerializeField] private GameObject[] _AgentHitboxes;
    [SerializeField] private GameObject _LOSSensor; // Destroyed at death

    [Header("Animation Refs")]
    [SerializeField] private Animator _Animator;
    [SerializeField] private SkinnedMeshRenderer _SkinnedMesh;
    [SerializeField] private RuntimeAnimatorController[] _IdleAnimations;
    [SerializeField] private RuntimeAnimatorController[] _WalkAnimations;
    [SerializeField] private RuntimeAnimatorController[] _RunAnimations;
    [SerializeField] private RuntimeAnimatorController[] _MeleeAnimations;
    [SerializeField] private RuntimeAnimatorController[] _DeathStealthAnimations;

    [Header("Patrolling")]
    [SerializeField] private Transform[] _PatrolPoints;
    [SerializeField] private int _PatrolPoint = 0; // Used for rare cases

    [Header("Inverse Kinematics")]
    [SerializeField] private Transform _HeadLookTarget;
    [SerializeField] private Transform _HeldEquipment; // For code-driven animation, repositions based on state
    [SerializeField] private Transform[] _RightLeftHand;
    [SerializeField] private Transform[] _EquipmentStates; // Place one for reach state, recommended you use disabled objects
    [SerializeField] private Transform[] _EquipmentRightHandStates;
    [SerializeField] private Transform[] _EquipmentLeftHandStates;
    [SerializeField] private MultiAimConstraint _HeadIK;
    [SerializeField] private TwoBoneIKConstraint[] _ArmIK; // Right and then Left arm, used for melee - UNITY BUG: EACH TIME THE ANIMATION CHANGES, THE IK WEIGHT RESETS TOO!

    [Header("Weapon Info")]
    [SerializeField] private GameObject _WeaponPrefab; // What is dropped when the agent dies (you can pick up enemy weapons, as it should be)
    [SerializeField] private float _FireRate = 0.25f;
    [SerializeField] private float _Damage = 1f;
    [SerializeField] private float _ImpactForce = 5f;
    [SerializeField] private Transform _BarrelTip;
    [SerializeField] private ParticleSystem _GunSmoke;
    [SerializeField] private AudioSource _GunSFX; // Feed the clip manually, I dont have the budget for more than one sfx per gun anyways...
    [SerializeField] private float[] _GunSFXPitchVariationMinMax = {0.85f, 1.25f};
    [SerializeField] private GameObject _BloodPool;

    [Header("DEBUG")]
    [SerializeField] private int _BehaviourState = 0; // IDLE - INVESTIGATING - COMBAT - MELEE - DEATH
    [SerializeField] private SpriteRenderer _StateIndicator;
    [SerializeField] private bool _IsMelee = false;
    [SerializeField] private int _DeathFlashCount = 0;
    
    /// INTERNAL VARIABLES
    private Vector3 _TargetLastLocation;
    private bool _RedundancyPlayerVisibilityCheck; // A redundancy check to ensure the AI is aware of the player's visibility in spite of melee state
    private int _SelectedIdleCombatStance; // Prevents the AI from changing combat stance when already in one


    /// GENERICS
    private void Awake()
    {
        IdleState();
        if (_PatrolPoints.Length > 1) {InvokeRepeating("Patrol", 0f, .5f);}
    }

    private void SetEquipmentIK()
    {
        if (_BehaviourState != 2) {_HeadLookTarget.transform.localPosition = new Vector3(0f, 1.5f, 1.5f);}

        _HeldEquipment.transform.position = _EquipmentStates[_BehaviourState].transform.position;
        _HeldEquipment.transform.rotation = _EquipmentStates[_BehaviourState].transform.rotation;

        _RightLeftHand[0].transform.position = _EquipmentRightHandStates[_BehaviourState].transform.position;
        _RightLeftHand[0].transform.rotation = _EquipmentRightHandStates[_BehaviourState].transform.rotation;

        _RightLeftHand[1].transform.position = _EquipmentLeftHandStates[_BehaviourState].transform.position;
        _RightLeftHand[1].transform.rotation = _EquipmentLeftHandStates[_BehaviourState].transform.rotation;
    }

    /// IDLE BEHAVIOUR
    private void IdleState()
    {
        _StateIndicator.color = Color.green;
        _BehaviourState = 0;

        SetEquipmentIK();
        _Pathfinder.speed = _AgentSpeeds[0];
        
        if (_PatrolPoints.Length > 1)
        {
            float BreakTime = Random.Range(0f, 12f);
            if (BreakTime > 4.5f)
            {
                _Animator.runtimeAnimatorController = _IdleAnimations[Random.Range(0, _IdleAnimations.Length)];
                Invoke("PatrolToNextNode", BreakTime);
                return;
            }
            else {PatrolToNextNode(); return;}
        }

        _Animator.runtimeAnimatorController = _IdleAnimations[Random.Range(0, _IdleAnimations.Length)];
    }

    /// PATROLLING BEHAVIOUR
    private void Patrol() 
    {
        if (Vector3.Distance(_Agent.transform.position, _PatrolPoints[_PatrolPoint].position) < 0.5f && _BehaviourState == 0)
        {
            if (_PatrolPoint + 1 >= _PatrolPoints.Length)   {_PatrolPoint = 0;}
            else                                            {_PatrolPoint++;}

            IdleState();
        }
    } 

    private void PatrolToNextNode()
    {
        _Animator.runtimeAnimatorController = _WalkAnimations[Random.Range(0, _WalkAnimations.Length)];
        _Pathfinder.destination = _PatrolPoints[_PatrolPoint].position;
    }

    /// INVESTIGATION AND CHASE BEHAVIOUR
    public void LOSLostPlayer(GameObject player, Sensor LOSFOV)
    {
        _RedundancyPlayerVisibilityCheck = false;
        if (_BehaviourState == 3) {return;} // Prevents LOS from affecting melee state (only affected by distance)

        _StateIndicator.color = Color.yellow;
        _TargetLastLocation = player.transform.position;
        _BehaviourState = 1;

        SetEquipmentIK();
        _Pathfinder.speed = _AgentSpeeds[1];

        Investigate();
    }

    private void Investigate()
    {
        if (_Pathfinder.pathStatus == NavMeshPathStatus.PathPartial) {Debug.LogError("AI is unable to reach destination, falling back to idle state!"); IdleState(); return;} // Prevents the AI from running into walls indefinitely - Shoutout to MathiasDG

        if (_BehaviourState == 1)
        {
            if (Vector3.Distance(_Agent.transform.position, _TargetLastLocation) > 1.5f)    {Invoke("Investigate", 0.5f);}
            else                                                                            {IdleState(); return;}
        }

        _Animator.runtimeAnimatorController = _RunAnimations[Random.Range(0, _RunAnimations.Length)];
        _Pathfinder.destination = _TargetLastLocation;
    }

    /// COMBAT BEHAVIOUR
    public void LOSFoundPlayer(GameObject player, Sensor LOSFOV)
    {
        _RedundancyPlayerVisibilityCheck = true;
        if (_BehaviourState == 3) {return;}

        _StateIndicator.color = Color.red;
        _BehaviourState = 2;

        _Pathfinder.speed = _AgentSpeeds[1];
        SetEquipmentIK();
        _SelectedIdleCombatStance = Random.Range(0, _IdleAnimations.Length);
        CombatPathfinding();
        CombatHeadIK();
        CancelInvoke("EngageCombat"); // Prevents invokes from piling up if the player begins to peek around thin objects.
        EngageCombat();
    }

    private void EngageCombat()
    {
        _HeldEquipment.LookAt(_PlayerTorso.transform.position);
        _Agent.transform.rotation = Quaternion.RotateTowards(_Agent.transform.rotation, Quaternion.LookRotation(_PlayerTorso.transform.position - _Agent.transform.position), Time.deltaTime * 360);
        
        _GunSFX.pitch = Random.Range(_GunSFXPitchVariationMinMax[0], _GunSFXPitchVariationMinMax[1]);
        _GunSFX.PlayOneShot(_GunSFX.clip);
        _GunSmoke.Clear(true); _GunSmoke.Play(true); // Gun goes boom kapow bazinga (this is equivalent to resimulating, just worse)
        
        // Originally I wanted delayed raycasting like the one in Blood.
        // But I am not a very good programmer and finding a way to do this without using corutines or pooling transforms in a really inefficient way is beyond me.
        // The code below is how I was delaying it, only issue is that Invoke cannot contain arugments (such as where the player was at that time).
        // Invoke("PerformBulletFunction", Vector3.Distance(_BarrelTip.transform.position, _PlayerTorso.transform.position) / 50); // Tweak the division to adjust the delay (10 - 100)
        // Sorry Ethan.

        // This used to utilize a raycast, but thats not really necessary thanks to the LOS check. Plus this way the player does not cheese it by blocking their chest with a random phys object.
        /* // OLD CODE:
        RaycastHit hit;
        Debug.DrawRay(_BarrelTip.transform.position, _PlayerTorso.transform.position - _BarrelTip.transform.position, Color.red, _FireRate);
        if (Physics.Raycast(_BarrelTip.transform.position, _PlayerTorso.transform.position - _BarrelTip.transform.position, out hit, Mathf.Infinity, _RaycastMask) && hit.collider.tag == "Player")
        {
            _PlayerRB.AddForce(_BarrelTip.forward * _ImpactForce, ForceMode.VelocityChange);
        }
        */
        _PlayerRB.AddForce(_BarrelTip.forward * _ImpactForce, ForceMode.VelocityChange); // This forcemode seems to be the best for the physics system, even if a bit bumpy

        if (_BehaviourState == 2) {Invoke("EngageCombat", _FireRate);}
    }

    private void CombatPathfinding()
    {
        float DistanceFromPlayer = Vector3.Distance(_Agent.transform.position, _PlayerTorso.transform.position);
        if (DistanceFromPlayer < 1.25)
        {
            _BehaviourState = 3;
            
            MeleePathfinding();
            return;
        }

        if (DistanceFromPlayer < 10)
        {
            _Pathfinder.destination = _Agent.transform.position;
            _Animator.runtimeAnimatorController = _IdleAnimations[_SelectedIdleCombatStance];
        }

        else
        {
            _Animator.runtimeAnimatorController = _RunAnimations[Random.Range(0, _RunAnimations.Length)];
            _Pathfinder.destination = _PlayerTorso.transform.position;
        }

        if (_BehaviourState == 2) {Invoke("CombatPathfinding", 1f);}
    }

    private void CombatHeadIK()
    {
        _HeadLookTarget.transform.position = _PlayerTorso.transform.position;
        if (_BehaviourState == 2) {Invoke("CombatHeadIK", 0.5f);}
    } 

    /// MELEE BEHAVIOUR
    private void MeleePathfinding()
    {
        float DistanceFromPlayer = Vector3.Distance(_Agent.transform.position, _PlayerTorso.transform.position);

        if (_IsMelee == false)
        {
            if (DistanceFromPlayer < 1.25)
            {
                _IsMelee = true;
                IniciateMeleeAttack();

                if (_BehaviourState == 3) {Invoke("MeleePathfinding", .5f);}
                return;
            }

            if (DistanceFromPlayer > 6.5)
            {
                if (_RedundancyPlayerVisibilityCheck == true)
                {
                    _BehaviourState = 2;
                    LOSFoundPlayer(_PlayerTorso, null);
                }
                else
                {
                    _BehaviourState = 1;
                    LOSLostPlayer(_PlayerTorso, null);
                }

                return;
            }
            else
            {
                _Animator.runtimeAnimatorController = _RunAnimations[Random.Range(0, _RunAnimations.Length)];
                _Pathfinder.destination = _PlayerTorso.transform.position;
                _ArmIK[1].weight = 0;
            }
        }

        if (_BehaviourState == 3) {Invoke("MeleePathfinding", .5f);}
    }

    private void IniciateMeleeAttack() // Animation Control
    {
        SetEquipmentIK();
        _Agent.transform.rotation = Quaternion.RotateTowards(_Agent.transform.rotation, Quaternion.LookRotation(_PlayerTorso.transform.position - _Agent.transform.position), 360);
        _Animator.runtimeAnimatorController = _MeleeAnimations[Random.Range(0, _MeleeAnimations.Length)];
        _ArmIK[1].weight = 0; // Put after new animation begins, IK gets reset otherwise (engine bug)

        float AnimationLength = _Animator.GetCurrentAnimatorStateInfo(0).length;
        Invoke("DealMeleeDamage", AnimationLength / 1.5f); // Used to half-sync the animation to the attack due to the poor quality of the available anims
        Invoke("EndMeleeAttack", AnimationLength);
    }

    private void DealMeleeDamage() // Damage and Physics
    {
        // Note: ~15000 is when stuff starts to move, based on current friction model (19-5-20)
        _PlayerRB.AddExplosionForce(24000f, _Agent.transform.position, Mathf.Infinity, .125f); // Friction overwrite not needed because of Mathf.Infinity ensuring static force
    }

    private void EndMeleeAttack()
    {
        _Animator.runtimeAnimatorController = _IdleAnimations[Random.Range(0, _IdleAnimations.Length)];
        _ArmIK[1].weight = 1; // Not necessary but added in case the bug gets fixed. Added SimonBZ on linkedin to let him know.

        _IsMelee = false;
    }


    /// DEATH AND DAMAGE STATES
    public void ReactToDamage()
    {
        // Not much for now, but its honest work.
        if (_BehaviourState == 0 || _BehaviourState == 1)
        {
            _Agent.transform.rotation = Quaternion.RotateTowards(_Agent.transform.rotation, Quaternion.LookRotation(_PlayerTorso.transform.position - _Agent.transform.position), 75f); // Prevents the agent from aiming through his chest.
            LOSFoundPlayer(_PlayerTorso, null);
        }
    }

    public void Death(int FinalHitPoint)
    {
        CancelInvoke(); // A bit hacky. But I rather do this that create another script.
        foreach (GameObject hitbox in _AgentHitboxes) {Destroy(hitbox);}
        Destroy(_LOSSensor);


        _StateIndicator.color = Color.black;
        _BehaviourState = 4;

        _Pathfinder.isStopped = true;
        _Animator.runtimeAnimatorController = _DeathStealthAnimations[Random.Range(0, _DeathStealthAnimations.Length)]; // Using wrong animations, change later.
        _ArmIK[0].weight = 0;
        _ArmIK[1].weight = 0;
        _HeadIK.weight = 0; // Used to also reposition head to last looked-at position, but it proved too overcomplicated.

        // Spawns pool of blood at foot of agent (might cause issues on ledges), blood behaviour scrip takes care of the rest.
        // Blood pool spawn position based on last hitbox to recieve damage (the one to call this function), 0 - 2 is head, torso, legs
        Vector3 LocalBloodOffset = new Vector3(_Agent.transform.localPosition.x, _Agent.transform.localPosition.y + 0.001f, _Agent.transform.localPosition.z);
        if      (FinalHitPoint == 0) {LocalBloodOffset = transform.TransformPoint(-1.25f, 0.001f, -0.7f);}
        else if (FinalHitPoint == 1) {LocalBloodOffset = transform.TransformPoint(-0.725f, 0.001f, -0.485f);}
        Instantiate(_BloodPool, LocalBloodOffset, Quaternion.Euler(-90f, 0f, Random.Range(0f, 360f)));

        _HeldEquipment.gameObject.SetActive(false); // Hides rendered weapon
        Instantiate(_WeaponPrefab, _HeldEquipment.position, _HeldEquipment.rotation); // Spawns real weapon at position

        Invoke("DeathFlash", 4.5f);
    }

    private void DeathFlash() // Retro style flash before dissapearing (much cheaper than blood pools and body discovery behaviours)
    {
        _SkinnedMesh.enabled = !_SkinnedMesh.enabled;

        if (_DeathFlashCount >= 18)  {Destroy(_Agent);}
        else                        {_DeathFlashCount++; Invoke("DeathFlash", 0.25f - ((25 - _DeathFlashCount) / 100));} // Speeds up as final destruction approaches.
    }
}
