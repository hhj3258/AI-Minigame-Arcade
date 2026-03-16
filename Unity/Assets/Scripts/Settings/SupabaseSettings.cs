using UnityEngine;

[CreateAssetMenu(fileName = "SupabaseSettings", menuName = "Settings/SupabaseSettings")]
public class SupabaseSettings : ScriptableObject
{
    [SerializeField]
    private string _projectUrl = "https://cvgdzkbvqjfvmqieedwg.supabase.co";

    [SerializeField]
    private string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImN2Z2R6a2J2cWpmdm1xaWVlZHdnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzM2MzE4OTksImV4cCI6MjA4OTIwNzg5OX0.dRHAcmO_1Gt17OLjFNLQJBA4EgU8UQhRPfFtvvK25_A";

    [SerializeField]
    private int _timeoutSeconds = 30;

    public string ProjectUrl => _projectUrl;
    public string AnonKey => _anonKey;
    public int TimeoutSeconds => _timeoutSeconds;
}
