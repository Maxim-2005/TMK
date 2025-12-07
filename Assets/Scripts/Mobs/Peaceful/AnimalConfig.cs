using UnityEngine;

[CreateAssetMenu(fileName = "New Animal Config", menuName = "Animals/Animal Config")]
public class AnimalConfig : ScriptableObject
{
    [Header("Movement Settings")]
    public float wanderSpeed = 2f;
    public float rotationSpeed = 5f;
    public float acceleration = 8f;
    public float stoppingDistance = 0.5f;
    
    [Header("Wandering Settings")]
    public float wanderRadius = 10f;
    public float minWanderDistance = 3f;
    public float wanderTimeout = 10f;
    public bool useStartPosition = true;
    
    [Header("Behavior Times")]
    public float minWaitTime = 3f;
    public float maxWaitTime = 5f;
    public float sleepDuration = 10f;
    public float pauseBetweenActions = 0.5f;
    public float maxRotationTime = 1f;
    
    [Header("Behavior Probabilities")]
    [Range(0f, 1f)] public float wanderChance = 0.6f;
    [Range(0f, 1f)] public float sleepChance = 0.1f;
    
    [Header("Behavior Pattern")]
    public string behaviorPattern = "121212123"; //1-бродить 2-ждать 3-спать
    public bool useRandomPattern = true;
}