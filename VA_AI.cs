using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Gives script acces to the engine AI library
using System.Linq; // Voice recognition dictionary
using UnityEngine.Windows.Speech; // Courtesy of https://lightbuzz.com/speech-recognition-unity/

[RequireComponent (typeof (NavMeshAgent))]
public class VA_AI : MonoBehaviour
{
	public NavMeshAgent pathfinder;
	Transform target;
	
	// Speech recognition system
	public string[] keywords = new string[] { "Follow", "Defend"};
    public ConfidenceLevel confidence = ConfidenceLevel.Low;
	
	protected PhraseRecognizer recognizer;
    protected string word = "word";
	
	// AI Behavioural states
	public enum State {
		Defend,
		Follow,
	};
	
	State currentState;
	
    void Start()
    {
        pathfinder.enabled = true; // Enables NavMeshAgent upon spawn
        pathfinder = GetComponent<NavMeshAgent> ();
		target = GameObject.FindGameObjectWithTag("VATarget").transform; // Sets AI target
		
		StartCoroutine (Follow());
		currentState = State.Follow;
		
		// Keyword Recognizer
		if (keywords != null)
        {
            recognizer = new KeywordRecognizer(keywords, confidence);
            recognizer.OnPhraseRecognized += Recognizer_OnPhraseRecognized;
            recognizer.Start();
        }
		
		// Should always be last
		pathfinder.enabled = true; // Enables NavMeshAgent upon spawn
    }

	private void Recognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        word = args.text;
		Debug.Log("Voice Recognizer Command: " + word);
    }

    void Update()
    {
        switch (word)
        {
			case "Follow":
                StartCoroutine (Follow());
				currentState = State.Follow;
				word = "NullValue"; // Prevents an infinite loop
                break;
			
			case "Defend":
                StartCoroutine (Defend());
				currentState = State.Defend;
				word = "NullValue"; // Prevents an infinite loop
                break;
		}
    }
	
	// Optimized AI path recalculator
	IEnumerator Follow()
	{
		float refreshRate = .35f; // Sets the refresh rate in seconds (Recommended: .10f - 1)
		pathfinder.isStopped = false; // Reactivates pathfinding
		
		while (target !=null) {
			if (currentState == State.Follow) {
				Vector3 targetPosition = new Vector3(target.position.x, 0, target.position.z);
				pathfinder.SetDestination(targetPosition);				
				yield return new WaitForSeconds(refreshRate);
			}
			else {
				yield return null; // Prevents a total nuclear meltdown by the system (Prevents the AI from getting stuck on the while statement) (Thanks to Iki once again)
			}
		}
	}
	
	IEnumerator Defend()
	{
		pathfinder.isStopped = true; // Deactivates pathfinding
		yield return null; // Prevents a total nuclear meltdown by the system (Prevents the AI from getting stuck on the while statement) (Thanks to Iki once again)
	}
	
	private void OnApplicationQuit()
	{
		if (recognizer != null && recognizer.IsRunning)
		{
			recognizer.OnPhraseRecognized -= Recognizer_OnPhraseRecognized;
			recognizer.Stop();
		}
	}
}