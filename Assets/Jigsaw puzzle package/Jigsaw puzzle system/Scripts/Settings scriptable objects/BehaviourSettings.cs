using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Behaviour Settings")]
public class BehaviourSettings : ScriptableObject
{
    [Header("Snap settings")]
    public float snapDistanceRatio = 0.25f;
    public float snapDistanceOnLoad = 0.001f;

    [Header("Scatter settings")]
    public float scatterDelay = 1f;
    public float scatterDurationPerPiece = 0.1f;
    public float inputDelay = 2f;
    public AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Puzzle completion effect settings")]
    public float effectDuration = 3f;
    public float particleRadiusBuffer = 0.25f;
    public Vector2 particleRateMinMax = new Vector2(200, 2000);
}
