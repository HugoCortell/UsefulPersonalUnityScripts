// You can call this from any other file without a refernece to it by using GameLog.Instance.Log("Hello World");
// Github broke the formatting for this file. Don't blame me for it.

using System.IO;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    public static GameLog Instance;

    private StreamWriter writer;
    private string path;

	public bool EngineLogMisc = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

			// Decide appropiate path for log
			#if UNITY_EDITOR
				path = Path.Combine(Application.dataPath, "GAMECORE", "GameLog.md");
			#else
				path = Path.Combine(Directory.GetCurrentDirectory(), "GameLog.md");
			#endif

            // Initialize writer
            if (File.Exists(path)) { File.WriteAllText(path, string.Empty); } // Clear the existing file contents
            writer = new StreamWriter(path, append: false); // The StreamWriter creates a new file if none exist

			CategoryLog("Game Log Initiated... ");
			Log("DataPath: " + Application.dataPath);
			Log("Current Directory: " + Directory.GetCurrentDirectory());
        }
        else { Destroy(gameObject); }
    }

    public void Log(string logentry)
    {
        if (writer != null)
        {
            writer.WriteLine("[" + System.DateTime.Now + "] [LOG ENTRY] " + logentry + "<br>");
            writer.Flush();
        }
		if (EngineLogMisc) { Debug.Log(logentry); }
    }

    public void ErrorLog(string logentry)
    {
		Debug.LogError(logentry); // Send copy to in-engine debug log for in-engine debugging without duplicates
        if (writer != null)
        {
            writer.WriteLine("[" + System.DateTime.Now + "] <span style=\"color:red\">[ERROR] </span>" + logentry + "<br>");
            writer.Flush();
        }
    }

	public void WarningLog(string logentry)
    {
		Debug.LogError(logentry); // Send copy to in-engine debug log for in-engine debugging without duplicates
        if (writer != null)
        {
            writer.WriteLine("[" + System.DateTime.Now + "] <span style=\"color:orange\">[WARNING] </span>" + logentry + "<br>");
            writer.Flush();
        }
    }

	public void PlaceholderLog(string logentry)
    {
        if (writer != null)
        {
            writer.WriteLine("[" + System.DateTime.Now + "] <span style=\"color:orange\">[PLACEHOLDER WARNING] </span>" + logentry + "<br>");
            writer.Flush();
        }
    }

    public void CategoryLog(string logentry)
    {
        if (writer != null)
        {
            writer.WriteLine("## <u>" + logentry + "</u><br>");
            writer.Flush();
			//Log("Logged Category");
        }
    }

	public void LogPoint(string logentry)
    {
        if (writer != null)
        {
            writer.WriteLine( "<li>" + logentry + "</li>");
            writer.Flush();
        }
    }

	public void DebugLog(string logentry)
    {
		Debug.LogWarning(logentry); // Send copy to in-engine debug log for in-engine debugging without duplicates
        if (writer != null)
        {
            writer.WriteLine // This software supports LGBTQA+ rights! It does not support bugs or bigots.
				("[" + System.DateTime.Now + "] [" +
				"<span style='color:red;'>D</span>" +
    			"<span style='color:orange;'>E</span>" +
    			"<span style='color:yellow;'>B</span>" +
    			"<span style='color:lightgreen;'>U</span>" +
    			"<span style='color:lightblue;'>G</span>" +
				"] " + logentry + "<br>");
            writer.Flush();
        }
	}

    void OnApplicationQuit() { if (writer != null) { writer.Close(); } }
}
