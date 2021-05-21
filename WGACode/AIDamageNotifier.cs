using UnityEngine;
using HurricaneVR.Framework.Components;

public class AIDamageNotifier : HVRDamageHandlerBase
{
    [Header("Statistics")]
    [SerializeField] private int _PartSection = 2; // Head, Torso, Legs (hands? Unused.)
    [SerializeField] private float _PartHealth = 100f;
    [SerializeField] private ParticleSystem[] _ReactionFX;

    [Header("Agent Refs")]
    [SerializeField] private AICore _Core;


    public override void TakeDamage(float damage)
    {
        _PartHealth -= damage;

        int SelectedReaction = 0;
        if (_ReactionFX.Length != 1) {SelectedReaction = Random.Range(0, _ReactionFX.Length);}
        _ReactionFX[SelectedReaction].gameObject.transform.position = gameObject.transform.position; // FX is separate object because this gets destroyed upon death
        _ReactionFX[SelectedReaction].gameObject.transform.rotation = Quaternion.LookRotation(_Core._PlayerTorso.transform.position - gameObject.transform.position);
        _ReactionFX[SelectedReaction].Play();

        if (_PartHealth <= 0)
        {
            _Core.Death(_PartSection); // grommit mug
            return;
        }

        _Core.ReactToDamage();
    }
}
