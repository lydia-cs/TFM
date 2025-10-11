using UnityEngine;

/// Interface for dynamically loadable models
public interface ILoadableModel
{
    string Model { get; }   // Model
    string Id { get; }      // Unique identifier
}



