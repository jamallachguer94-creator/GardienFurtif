using UnityEngine;

// A noise = where it happened + how far it can be heard.
// Jamal's NoiseEmitter will create these. My HearingSensor will receive them.
public readonly struct NoiseEvent
{
    public readonly Vector3 Position; // where the noise happened
    public readonly float Radius;     // how far (in metres) it can be heard

    public NoiseEvent(Vector3 position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}