using UnityEngine;

[CreateAssetMenu(fileName = "ApiSettings", menuName = "AIMiniArcade/ApiSettings")]
public class ApiSettings : ScriptableObject
{
    public string Endpoint = "https://api.anthropic.com/v1/messages";
    public string Model = "claude-haiku-4-5-20251001";
    public int MaxTokens = 1024;
    public int TimeoutSeconds = 30;
}

