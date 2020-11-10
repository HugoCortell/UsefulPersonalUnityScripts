using UnityEngine;

public class BCE9TurretRemapper : MonoBehaviour
{
    // ASSUMPTIONS:
    // * THIS IS ALWAYS GOING TO BE ON THE X AXIS, MIDPOINT IS ZERO.

    // Actual gun elevations
    [SerializeField] private float _MinimunElevation = 10;
    [SerializeField] private float _MaximunElevation = -20;

    // Rotator's hinge maximuns
    [SerializeField] private float _RotationMinimun = 180;
    [SerializeField] private float _RotationMaximun = -180;

    public void RemapTurretRotation(float _RotatorValue) // Is dynamic - called via event.
    {
        transform.rotation = Quaternion.Euler((_RotatorValue - _RotationMinimun) / (_RotationMaximun - _RotationMinimun) * (_MaximunElevation - _MinimunElevation) + _MinimunElevation, 0, 0);
    } // NOTE: transform.rotate can go to hell
}
