using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : MonoBehaviour 
{
    private float _FadeRate = 1f;
    private float _DesiredAlpha; // Kept separate from target alpha just to keep things clean

    [SerializeField]
    private bool _DebugCheck = false;

    private Image _image;
    private float _TargetAlphaValue;

    private void Start()
    {
        _image = GetComponent<Image>();
        Material instantiatedMaterial = Instantiate<Material>(_image.material);
        _image.material = instantiatedMaterial;
        _TargetAlphaValue = _image.material.color.a;

        if (_DebugCheck == true) // If option is turned on, then call the fade function in a second after start.
        {
            startImageFade(3f, 0f); // Leave this section of the script intact for future reference, please.
        }

    }
    
    // Call to fade UI element
    public void startImageFade(float _FadeRate, float _DesiredAlpha)=> StartCoroutine(FadeUIElement(_FadeRate, _DesiredAlpha)); // PUBLIC, CALLED BY OTHER SCRIPTS

    IEnumerator FadeUIElement(float _FadeRate, float _DesiredAlpha) 
    {
        _TargetAlphaValue = _DesiredAlpha;
        Color curColor = _image.color;
        while(Mathf.Abs(curColor.a - _TargetAlphaValue) > 0.0001f) 
        {
            curColor.a = Mathf.Lerp(curColor.a, _TargetAlphaValue, _FadeRate * Time.deltaTime);
            _image.color = curColor;
            yield return null;
        }
    }
}