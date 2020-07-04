using UnityEngine;
using System.Linq; // Voice recognition dictionary
using UnityEngine.Windows.Speech; // Courtesy of https://lightbuzz.com/speech-recognition-unity/

// Updated version of the VA_AI.cs file, now with better coding practices.
public class AbilityInvoker : MonoBehaviour
{
	// Speech recognition system
	public string[] _Keywords = new string[] { "Microphone", "Test"};
    public ConfidenceLevel _Confidence = ConfidenceLevel.Low;
	
	protected PhraseRecognizer _Recognizer;
    protected string _WordString = "word";
	
    void Start()
    {
        Debug.Log("<color=red>Voice Recognizer is ON</color>");

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
        Debug.Log("<color=green>Voice Recognizer Command: </color>" + _WordString);
    }

    void Update() // Test if the switch is a if true, if so, make it a delayed update.
    {
        switch (_WordString)
        {
			case "Microphone":
                Debug.Log("aaaaaaaaaaaaaaaa");
				_WordString = "NullValue"; // Prevents an infinite loop
                break;
			
			case "Test":
                Debug.Log("bbbbbbbbbbbbbbbb");
				_WordString = "NullValue"; // Prevents an infinite loop
                break;
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