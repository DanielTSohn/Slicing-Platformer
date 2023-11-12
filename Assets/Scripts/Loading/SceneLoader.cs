using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("The event used to call scene loading"), SerializeField] private StringGameEvent loadEvent;
    [Tooltip("The curve from 1 to 0 on how the transition canvas alpha fades on load in"), SerializeField] private AnimationCurve fadeInCurve;
    [Tooltip("The curve from 0 to 1 on how the transition canvas alpha fades on load out"), SerializeField] private AnimationCurve fadeOutCurve;
    [Tooltip("The canvas group of the transition canvas, used for covering scene loads"), SerializeField] private CanvasGroup transitionCanvasGroup;

    public bool Loading { get; private set; }
    public string TargetScene { get; private set; }
    public string LoadingSceneName { get => loadingSceneName; private set => loadingSceneName = value; }
    [Tooltip("The name of the loading screne this script should use"), SerializeField] private string loadingSceneName;
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        // Singleton handling
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            transform.parent = null;
            Instance.loadEvent.OnOneParameterEventTriggered += LoadSceneByName;
            DontDestroyOnLoad(this);
        }
    }

    /// <summary>
    /// Load a scene with the given name if it is in the build order, calls the loading screen as a buffer between them
    /// </summary>
    /// <param name="scene">The target scene to load by string name</param>
    public void LoadSceneByName(string sceneName) { SetTargetScene(sceneName); LoadScene(); }
    /// <summary>
    /// Loads the target scene saved to this loader, calls the loading screen as a buffer between them
    /// </summary>
    public void LoadScene() { StartCoroutine(BufferLoading()); }

    /// <summary>
    /// Set target scene to load when LoadScene() is Called
    /// </summary>
    /// <param name="sceneName"></param>
    public void SetTargetScene(string sceneName)
    {
        TargetScene = sceneName;
    }

    /// <summary>
    /// Masks the screen with the transition canvas
    /// </summary>
    private void MaskScreen()
    {
        transitionCanvasGroup.blocksRaycasts = true;
        transitionCanvasGroup.alpha = 1;
    }


    /// <summary>
    /// Reveals screen from the transition canvas
    /// </summary>
    private void RevealScreen()
    {
        transitionCanvasGroup.alpha = 0;
        transitionCanvasGroup.blocksRaycasts = false;
    }


    /// <summary>
    /// Fades from current alpha to transparent
    /// </summary>
    private IEnumerator FadeIn()
    {
        /// Set to mask screen while fading and block raycasts
        MaskScreen();

        /// Iterate through animation curve to set alpha
        float startValue = transitionCanvasGroup.alpha;
        for (float time = 0; time < fadeInCurve.keys[^1].time; time += Time.fixedUnscaledDeltaTime)
        {
            transitionCanvasGroup.alpha = Mathf.Lerp(0, startValue, fadeInCurve.Evaluate(time));
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForEndOfFrame();

        RevealScreen();
        /// Set values to avoid floating point errors and enable raycasts
    }

    /// <summary>
    /// Fades from current alpha to blocking transition canvas
    /// </summary>
    /// <param name="loading">The async loading of the next scene</param>
    private IEnumerator FadeOut(AsyncOperation loading)
    {
        /// Don't allow other loading calls and stop early loading
        Loading = true;
        loading.allowSceneActivation = false;

        /// Wait until loading is done to fade out
        yield return new WaitUntil(() => loading.progress >= 0.9f);

        /// Set initial lerp state
        RevealScreen();
        /// Prevent UI actions while transitioning
        transitionCanvasGroup.blocksRaycasts = true;
        
        /// Iterate through animation curve to shift alpha
        float startValue = transitionCanvasGroup.alpha;
        for (float time = 0; time < fadeOutCurve.keys[^1].time; time += Time.fixedUnscaledDeltaTime)
        {
            transitionCanvasGroup.alpha = Mathf.Lerp(startValue, 1, fadeOutCurve.Evaluate(time));
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForEndOfFrame();
        
        /// Set final lerp state
        MaskScreen();

        loading.allowSceneActivation = true;
        Loading = false;
    }

    /// <summary>
    /// Buffer any loading requests until after finishing another load
    /// </summary>
    private IEnumerator BufferLoading()
    {
        if (Loading) { yield return new WaitWhile(() => Loading); }
        if (!string.IsNullOrEmpty(TargetScene))
        {
            StartCoroutine(LoadSceneAsync(SceneManager.LoadSceneAsync(loadingSceneName)));
        }
        else { Debug.LogError("Target scene not set"); }
    }

    /// <summary>
    /// Event stack from current scene to loading scene to target scene
    /// </summary>
    /// <param name="loading">Astnc loading to wait on</param>
    private IEnumerator LoadSceneAsync(AsyncOperation loading)
    {
        yield return StartCoroutine(FadeOut(loading));
        yield return StartCoroutine(FadeIn());
        
        /// Reset time scale to normal
        Time.timeScale = 1;

        AsyncOperation targetLoading = SceneManager.LoadSceneAsync(TargetScene);
        yield return StartCoroutine(FadeOut(targetLoading));
        yield return StartCoroutine(FadeIn());
    }
}