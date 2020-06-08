using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The improved music player system.
// Previous system did not work well with the rest of the code, so I just re-made it.

// Please dont touch it if you dont need to change anything. Audio is a fragile thing.
public class MusicPlayer : MonoBehaviour
{
    [Header("Audio References")] // Audio Source & Clip - THIS REQUIRES THE EXISTANCE OF BOTH SOURCES IN ORDER TO WORK!
    [SerializeField]
    private AudioSource _ASPrimary; // Primary Audio Source
    [SerializeField]
    private AudioSource _ASSecondary; // Secondary Audio Source

    [SerializeField]
    private AudioClip[] _Aclips; // List of audio clips (Any amount)


    [Header("Audio Settings")] // Audio Settings
    public float _AudioVolume = 1f; // Should stay at 1f, but does not matter that much.
    [SerializeField]
    private float _AudioFadeTime = 15f; // How long before it starts to fade - Should be a long value (25 - 5)


    [Header("Debug Settings")]
    [SerializeField]
    private bool _IsDebugFadeEnabled = false; // Enable to debug fading


    // Storage
    private int _FadeState = 0; // Controls fading status
    private int _CurrentValue;
    private int _TempRandomValue; // Used for checking if a song has already played previously.
    
    void Start()
    {
        // Firt time bootup
        _CurrentValue = Random.Range (0, _Aclips.Length);
        _ASPrimary.clip = _Aclips[_CurrentValue];
        _ASPrimary.Play(); // THANK YOU JAPAN! Had it not been for the japanese, I would not have known that PlayOneShot does not store clip data.

        _ASPrimary.volume = _AudioVolume;
        _ASSecondary.volume = 0f;
        Invoke("S1E", _ASPrimary.clip.length - _AudioFadeTime);
    }


    void Update() 
    {
        if (_FadeState == 1)
        {
            _ASPrimary.volume = Mathf.Lerp(_ASPrimary.volume, 0, Time.deltaTime / _AudioFadeTime);
            _ASSecondary.volume = Mathf.Lerp(_ASSecondary.volume, _AudioVolume, Time.deltaTime / _AudioFadeTime);
        }
        
        if (_FadeState == 2)
        {
            _ASPrimary.volume = Mathf.Lerp(_ASPrimary.volume, _AudioVolume, Time.deltaTime / _AudioFadeTime);
            _ASSecondary.volume = Mathf.Lerp(_ASSecondary.volume, 0, Time.deltaTime / _AudioFadeTime);
        }

        if (_IsDebugFadeEnabled == true && _FadeState != 0)
        {
            Debug.Log(_ASPrimary.volume);
        }
    }

    void SetNewSong() // Ensures no song is played twice in a row.
    {
        _TempRandomValue = Random.Range (0, _Aclips.Length);
        while (_TempRandomValue == _CurrentValue)
        {
            _TempRandomValue = Random.Range (0, _Aclips.Length);
        }
        _CurrentValue = _TempRandomValue;
    }

    // HERE BE DRAGONS! These functions are hard to follow, tread carefuly.
    // Think of this as a series of logic gates instead of functions.
    void S1E()
    {
        Debug.Log("MUSIC PLAYER LOG: S1E");
        SetNewSong();

        _ASSecondary.clip = _Aclips[_CurrentValue];
        _ASSecondary.Play();

        _ASSecondary.volume = 0f;
        _FadeState = 1;
        // This exectured even though it had an if statement, so I had to manually disable it until I find a workaround.
        //if (_IsDebugFadeEnabled == true){Debug.Break();} // Pause for automated testing - Ignore.

        Invoke("S1ES", _AudioFadeTime);
    }
    void S1ES()
    {
        Debug.Log("MUSIC PLAYER LOG: S1ES");
        _ASPrimary.Stop();

        _FadeState = 0;
        _ASSecondary.volume = _AudioVolume;

        Invoke("S2E", _ASSecondary.clip.length - _AudioFadeTime);
    }

    void S2E()
    {
        Debug.Log("MUSIC PLAYER LOG: S2E");
        SetNewSong();

        _ASSecondary.clip = _Aclips[_CurrentValue];
        _ASPrimary.Play();

        _ASPrimary.volume = 0f;
        _FadeState = 2;

        Invoke("S2ES", _AudioFadeTime);
    }
     void S2ES()
    {
        Debug.Log("MUSIC PLAYER LOG: S2ES");
        _ASSecondary.Stop();

        _FadeState = 0;
        _ASPrimary.volume = _AudioVolume;

        Invoke("S1E", _ASPrimary.clip.length - _AudioFadeTime);
    }
}
