using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Game/Dialogue")]
public class DialogueData : ScriptableObject
{
    [TextArea(2, 5)]
    public string[] sentences;

    public float typeSpeed = 0.03f;
}