using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Voice recognition dictionary
using UnityEngine.Windows.Speech; // Courtesy of https://lightbuzz.com/speech-recognition-unity/

[RequireComponent(typeof(AudioSource))]
public class SpeechInteractionCORE : MonoBehaviour
{
    [Header("Array Spaghetti")] // Speech recognition system - Related arrays [ALL SHOULD MATCH YOU THICK BASTARD]
	[SerializeField] private string[] _Keywords; // Array with keywords (should match TMP text)
    [SerializeField] private bool[] _HasBeenCalled; // Default is false
    [SerializeField] private GameObject[] _KeyLines; // TMP game object array
    [SerializeField] private AudioClip[] _VoiceClips; // List of audio clips

    // Speech recognition system - The rest
    [Header("Voice Recog Settings")]
    public ConfidenceLevel _Confidence = ConfidenceLevel.Low;
	
	protected PhraseRecognizer _Recognizer;
    protected string _WordString = "word";

    [Header("Other")] // External stuffs
    [SerializeField] private AudioSource _AudioSource;

    // Non interfaced data
    private bool _IsReactEnable = false;
    
    private void Start()
    {
		// Keyword Recognizer
		if (_Keywords != null)
        {
            _Recognizer = new KeywordRecognizer(_Keywords, _Confidence);
            _Recognizer.OnPhraseRecognized += Recognizer_OnPhraseRecognized;
            _Recognizer.Start();
        }
    }

    private void Recognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        _WordString = args.text;
        
        if (_IsReactEnable == true) 
        {
            int _IndexRetrVal = System.Array.IndexOf(_Keywords, _WordString);

            if (_HasBeenCalled[_IndexRetrVal] == false)
            {
                _KeyLines[_IndexRetrVal].SetActive(false);
                _AudioSource.PlayOneShot(_VoiceClips[_IndexRetrVal]);

                _HasBeenCalled[_IndexRetrVal] = true;
            }

            _WordString = "NullValue"; // Prevents an infinite loop (Is there even one in this occasion??)
        }
    }

    // Trigger stuffs
    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "Player")
        {
            _IsReactEnable = true;
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.tag == "Player")
        {
            _IsReactEnable = false;
        }
    }

    private void OnApplicationQuit()
	{
		if (_Recognizer != null && _Recognizer.IsRunning)
		{
            Debug.Log("<color=red>Voice Recognizer is OFF</color>");
			_Recognizer.OnPhraseRecognized -= Recognizer_OnPhraseRecognized; // Is this necessary?
			_Recognizer.Stop();
		}
	}
}
