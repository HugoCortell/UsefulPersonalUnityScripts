#if false // Hack into not compiling

using UnityEngine;
using System.Collections;
// Just a place for me to store audio code for ease of reusage. Ingore.
public class GenericAudioLib : MonoBehaviour{}

// Shooting guns, AI or otherwise.
public class AUDIOLIBShooting
{
    [Header("Audio References")] // Audio Source & Clip
    [SerializeField]
    private AudioSource[] _ASources; // List of audio sources (Use 2 or more!)
    [SerializeField]
    private AudioClip[] _Aclips; // List of audio clips (Any amount)

    [Header("Audio Settings")] // Audio Settings
    [SerializeField]
    private float _AudioVolume = 1f;

    [SerializeField]
    private bool _IsRandomPitchEnabled = false;
    [SerializeField]
    private float _AudioPitchMin;
    [SerializeField]
    private float _AudioPitchMax;

    // Audio storage
    private int _ActiveAudioSource = 0; // Current audio source cycle

    void library()
    {
        // Audio Checks
        if (_ActiveAudioSource + 1 > _ASources.Length) // Check if int is overboard
        {
            _ActiveAudioSource = 0;
        }
        if (_IsRandomPitchEnabled == true) // Randomize Pitch if Enabled
        {
            _ASources[_ActiveAudioSource].pitch = (Random.Range(_AudioPitchMin, _AudioPitchMax));
        }

        // Audio play
        _ASources[_ActiveAudioSource].PlayOneShot(_Aclips[Random.Range (0, _Aclips.Length)], _AudioVolume);
        _ActiveAudioSource++;
    }
}

// TEMPLATE
public class AUDIOLIB
{
    void library
    {

    }
}
#endif
