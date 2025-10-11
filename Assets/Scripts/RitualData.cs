using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RitualData
{
    public List<RitualInfo> RitualInfo;      // Basic information
    public List<Music> Music;                // Music tracks associated with the ritual
    public List<Environment> Environments;   // Environments used in the ritual
    public List<Place> Places;               // Specific places within the ritual environment
    public List<Object> Objects;             // Objects that characters can interact with
    public List<Character> Characters;       // Characters participating in the ritual
    public List<Animation> Animations;       // Animation data for characters
    public List<Act> Acts;                   // Acts within the ritual
    public List<Play> Play;                  // Representing individual actions or sequences
    public List<Branch> Branches;            // Branches for conditional or alternative sequences

    // Convenience Find Methods
    public Character FindCharacter(string id)   // Finds a character by ID (case-insensitive)
    {
        return Characters.Find(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public Object FindObject(string id)         // Finds an object by ID (case-insensitive)
    {
        return Objects.Find(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public Place FindPlace(string id)           // Finds a place by ID (case-insensitive)
    {
        return Places.Find(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public Environment FindEnvironment(string id)   // Finds an environment by ID (case-insensitive)
    {
        return Environments.Find(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}



