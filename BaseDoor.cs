using UnityEngine;

public class BaseDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("This is the invisible object that the door will move towards in order to open.")]
    [SerializeField]
    private Transform _Target;
    [SerializeField]
    private float _MoveSpeed = 1;
    [Tooltip("Dictates how many seconds before the door resumes to its default state (0 = No)")]
    [SerializeField]
    private float _ResetTime = 5;

    [Header("Multi-Door Settings")] // Only supports up to two doors, I could use a list to make it infinite but honestly I dont think we will need more than two.
    [Tooltip("Is the door composed of a single object or is it two? (Turn OFF to set it to multi-door)")]
    [SerializeField]
    private bool _IsSingleDoor = true;
    [Tooltip("Drag the secondary door here")]
    [SerializeField]
    private GameObject _SecondaryDoor;
    [SerializeField]
    private Transform _SecondaryTarget;
    [SerializeField]
    private AudioSource _DoorAudioSource; // We dont need more than one sound, we could use pitch variation if needed.

    // Private stuffs
    private Vector3 _OriginalPos; // Do not influence outside of start function
    private Vector3 _SecondaryOrigin;

    [Header("DEBUG TOOLS")]
    public bool _DoorCall = false; // Public so it can be hacked
    [SerializeField]
    private bool _DoorReset = false;

    void Start()
    {
        _OriginalPos = this.transform.position;
        if (_IsSingleDoor == false)
        {
            _SecondaryOrigin = _SecondaryDoor.transform.position;
        }
    }

    void Update()
    {
        // Checks if door is to be opened
        if (_DoorCall == true)
        {
            // Moves door into position
            if (_IsSingleDoor == true)
            {
                transform.position = Vector3.MoveTowards(transform.position, _Target.position, _MoveSpeed * Time.deltaTime);
            }
            else 
            {
                // Moves second door first by one line, it may not seem like it, but it does make a difference.
                _SecondaryDoor.transform.position = Vector3.MoveTowards(_SecondaryDoor.transform.position, _SecondaryTarget.position, _MoveSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, _Target.position, _MoveSpeed * Time.deltaTime);
            }

            // Checks if door has reached desired position, then make it so that it stops moving the door.
            if (transform.position == _Target.position) 
            {
                if (_ResetTime > 0) // Starts the reset process if valid
                {
                    Invoke("DoorReset", _ResetTime);
                }
                _DoorCall = false;
            }
        }

        if (_DoorReset == true) // Same system as before but inverted
        {
            if (_IsSingleDoor == true)
            {
                transform.position = Vector3.MoveTowards(transform.position, _OriginalPos, _MoveSpeed * Time.deltaTime);
            }
            else 
            {
                // Moves second door first by one line, it may not seem like it, but it does make a difference.
                _SecondaryDoor.transform.position = Vector3.MoveTowards(_SecondaryDoor.transform.position, _SecondaryOrigin, _MoveSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, _OriginalPos, _MoveSpeed * Time.deltaTime);
            }

            // Checks if door has closed
            if (transform.position == _OriginalPos)
            {
                _DoorReset = false;
            }
        }
    }

    // Delayed action bool modification.
    private void DoorReset() => _DoorReset = true;

    // Collider stuff
    void OnTriggerStay(Collider _Target)
     {
         if(_Target.tag == "Player" && _DoorReset == false && _DoorCall == false) // TODO: ADD [OR] ENEMY TAG
         {
            _DoorCall = true;
         }
     }
}
