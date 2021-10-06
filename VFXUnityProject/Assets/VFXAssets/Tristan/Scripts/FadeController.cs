using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeController : MonoBehaviour
{
    [SerializeField] private Renderer _targetRenderer;
    [SerializeField] private float _fadeInTime = 0f;
    [SerializeField] private float _fadeInDuration = 0.1f;
    [SerializeField] private float _fadeOutTime = 60f;
    [SerializeField] private float _fadeOutDuration = 0.1f;

    private Material targetMaterial;
    private float currentAlpha;
    private Coroutine fadeInRoutine;
    private Coroutine fadeOutRoutine;

    private void OnEnable()
    {
        targetMaterial = _targetRenderer.material;
        currentAlpha = 1;
        SetAlpha();

        if (fadeInRoutine != null)
        {
            StopCoroutine(fadeInRoutine);
        }
        fadeInRoutine = StartCoroutine(FadeRoutine(_fadeInTime, _fadeInDuration, 0));

        if (fadeOutRoutine != null)
        {
            StopCoroutine(fadeOutRoutine);
        }
        fadeOutRoutine = StartCoroutine(FadeRoutine(_fadeOutTime, _fadeOutDuration, 1));
    }

    private void SetAlpha()
    {
        Color c = targetMaterial.color;
        c.a = currentAlpha;
        targetMaterial.color = c;
    }

    private IEnumerator FadeRoutine(float time, float fadeDuration, float targetAlpha)
    {
        yield return new WaitForSeconds(time);

        float startAlpha = currentAlpha;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / fadeDuration);
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, ratio);
            SetAlpha();
            yield return null;
        }
    }
}
