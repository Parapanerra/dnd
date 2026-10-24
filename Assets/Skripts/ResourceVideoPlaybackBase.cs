using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public abstract class ResourceVideoPlaybackBase : MonoBehaviour
{
    // Field names are retained for the existing dragon scene component.
    [SerializeField] protected Image dragonImage;
    [SerializeField] protected VideoClip breathClip;

    private VideoPlayer player;
    private RawImage videoImage;
    private RenderTexture videoTexture;
    private Coroutine fadeRoutine;

    internal void PlayOnce()
    {
        if (dragonImage == null || breathClip == null)
        {
            Debug.LogError("Resource video needs an image and a clip.", this);
            return;
        }

        if (player == null)
            CreateVideoImage();

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        player.Stop();
        videoImage.enabled = false;
        videoImage.color = Color.white;
        player.Prepare();
    }

    private void CreateVideoImage()
    {
        var overlay = new GameObject("Resource Video", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage), typeof(VideoPlayer));
        overlay.transform.SetParent(dragonImage.transform, false);

        var rect = (RectTransform)overlay.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        videoTexture = new RenderTexture((int)breathClip.width, (int)breathClip.height, 0);
        videoTexture.Create();

        videoImage = overlay.GetComponent<RawImage>();
        videoImage.texture = videoTexture;
        videoImage.raycastTarget = false;
        videoImage.enabled = false;

        player = overlay.GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = false;
        player.skipOnDrop = false;
        player.timeUpdateMode = VideoTimeUpdateMode.GameTime;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = videoTexture;
        player.clip = breathClip;
        player.sendFrameReadyEvents = true;
        player.prepareCompleted += OnPrepared;
        player.frameReady += OnFrameReady;
        player.loopPointReached += OnFinished;
        player.errorReceived += OnVideoError;
    }

    private void OnPrepared(VideoPlayer source)
    {
        source.Play();
    }

    private void OnFrameReady(VideoPlayer source, long frame)
    {
        videoImage.enabled = true;
    }

    private void OnFinished(VideoPlayer source)
    {
        fadeRoutine = StartCoroutine(FadeToStill());
    }

    private IEnumerator FadeToStill()
    {
        const float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            videoImage.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        videoImage.enabled = false;
        videoImage.color = Color.white;
        fadeRoutine = null;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        videoImage.enabled = false;
        Debug.LogError("Resource video failed: " + message, this);
    }

    protected virtual void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        if (player != null)
            player.Stop();
        if (videoImage != null)
            videoImage.enabled = false;
    }

    protected virtual void OnDestroy()
    {
        if (player != null)
        {
            player.prepareCompleted -= OnPrepared;
            player.frameReady -= OnFrameReady;
            player.loopPointReached -= OnFinished;
            player.errorReceived -= OnVideoError;
        }

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }
}
