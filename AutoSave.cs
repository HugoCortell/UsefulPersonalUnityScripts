using UnityEngine;

public class AutoSave : MonoBehaviour
{
    [SerializeField] private int _LevelID;
    
    private void Awake() {PlayerPrefs.SetInt("LevelCount", _LevelID);}
}

/* THEN USE THIS TO LOAD

private void Awake() {_CurrentLevel = PlayerPrefs.GetInt("LevelCount");}
SceneManager.LoadScene(_LevelStrings[_CurrentLevel]);

THATS IT, SIMPLE RIGHT? */
