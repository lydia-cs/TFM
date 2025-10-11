using System;
using UnityEngine;

/// Represents a choice branch in the ritual sequence
[Serializable]
public class Branch
{
    public string Id;              // Unique identifier
    public int StartingLine;       // Index of the line where the branch starts
    public int EndingLine;         // Index of the line where the branch ends
    public string ButtonText;      // Text displayed on the choice button
}


