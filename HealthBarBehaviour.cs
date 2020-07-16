using UnityEngine;
using TMPro;
using DG.Tweening;

public class HealthBarBehaviour : MonoBehaviour
{
    // AttackDelay
    [SerializeField] private float _Delay = 0.25f;
    protected bool _IsCycleLocked = false;

    // Healthstuffs
    [SerializeField] private int _Health = 100;
    protected int _HChange;

    [SerializeField] private float _PercentageSumTotal = 3f;

    // Does it count as hardcoded if it can be changed from the inspector and in-game?
    [SerializeField] private int _MINhealth = 0;
    [SerializeField] private int _MAXhealth = 100;

    //TMP refs
    [SerializeField] private GameObject _TMPObject;
    [SerializeField] private TMP_Text _TXT;

    //DEBUG
    public int _DealDamage = -50;

    void Start(){_TMPObject.SetActive(false);}

    void Update()
    {
        if (_IsCycleLocked == false) // Ensures that a new cycle only starts once the previous has finished (Change _Delay if this is too slow)
        {
            // Deal random health
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _HChange = Random.Range(-100, +100);

                _Health += _HChange;
                HandleText();
            }

            // Reset Health
            if (Input.GetKeyDown(KeyCode.R))
            {
                _MINhealth = 0;
                _MAXhealth = 100;
                _Health = 100;
            }

            // DEBUG: Input something in _DealDamage and then press the button to deal that damage
            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                _HChange = _DealDamage;

                _Health += _HChange;
                HandleText();
            }

            // Change MIN MAX
            // MIN
            if (Input.GetKeyDown(KeyCode.Keypad7)){_MINhealth += 5;}
            if (Input.GetKeyDown(KeyCode.Keypad1)){_MINhealth -= 5;}

            // MAX
            if (Input.GetKeyDown(KeyCode.Keypad9)){_MAXhealth += 5;}
            if (Input.GetKeyDown(KeyCode.Keypad3)){_MAXhealth -= 5;}
        }

        // ENFORCE MINMAX
        if (_MAXhealth < 5){_MAXhealth = 5;}
        if (_MINhealth < 0){_MINhealth = 0;}

        if (_Health > _MAXhealth){_Health = _MAXhealth;}
        if (_Health < _MINhealth){_Health = _MINhealth;}

        // Calculate HP percentage and tween bar
        
        // OLD CRAPPY CODE
        /*_PercentageSumTotal = (_Health / (_MAXhealth - _MINhealth)) * 100;
       _PercentageSumTotal = (Mathf.Clamp(_PercentageSumTotal, 0, 100) / 100) * 3;*/

       // NEW SEXY CODE
       _PercentageSumTotal = Mathf.InverseLerp(_MINhealth, _MAXhealth, _Health) * 3;

        transform.DOScaleX(_PercentageSumTotal, 1);
    }

    void HandleText()
    {
        _TMPObject.SetActive(true);
        _TXT.text = "" + _HChange; // Why convert when you can do ("" +), C# is trully a wonderful language

        if (_HChange > 0){_TXT.color = new Color32(0, 255, 0, 255);} // GREEN
        if (_HChange < 0){_TXT.color = new Color32(255, 0, 0, 255);} // RED
        if (_HChange == 0){_TXT.color = new Color32(255, 255, 255, 255);} // WHITE

        _IsCycleLocked = true;
        Invoke("HandleText2", _Delay); // Why use expensive wait commands or corutines when I can just do this!
    }

    void HandleText2() 
    {
        _TMPObject.SetActive(false);
        _IsCycleLocked = false;
    }
}
