using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Plays the approved button motion directly from small frame atlases.</summary>
public sealed class OverlayButtonVideoLoop : MonoBehaviour
{
    private const int FramesPerSecond = 30;
    private const int TotalFrames = 120;
    private const int FramesPerAtlas = 40;
    private const int IconSize = 96;
    private const int AtlasColumns = 11;

    // This order matches the columns written by build_overlay_button_atlases.py.
    private static readonly string[] SpriteNames =
    {
        "3156413215_6",         // Character
        "3156413215_7",         // Wild shape
        "3156413215_11",        // Magic
        "3156413215_8",         // Character information
        "3156413215_9",         // Spellbook
        "3156413215_5",         // Inventory
        "AlbumOverlayButton",
        "3156413215_10",        // Main menu
        "3156413215_4",         // Reset
        "3156413215_12",        // Menu launcher
        "CalculatorOverlayButton"
    };

    private static Texture2D[] atlases;
    private static float startedAt;
    private readonly List<ButtonView> buttonViews = new List<ButtonView>(AtlasColumns);
    private int displayedFrame = -1;

    private struct ButtonView
    {
        public Image source;
        public RawImage animation;
        public int column;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlayback()
    {
        atlases = null;
        startedAt = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrepareBeforeFirstScene()
    {
        LoadAtlases();
    }

    private static bool LoadAtlases()
    {
        if (atlases != null)
            return atlases.Length == TotalFrames / FramesPerAtlas;

        var loaded = new Texture2D[TotalFrames / FramesPerAtlas];
        for (var i = 0; i < loaded.Length; i++)
        {
            loaded[i] = Resources.Load<Texture2D>("OverlayButtonsAtlas" + i);
            if (loaded[i] == null)
            {
                Debug.LogWarning("Overlay button animation atlas is missing: " + i);
                return false;
            }
        }

        atlases = loaded;
        startedAt = Time.realtimeSinceStartup;
        return true;
    }

    private void Awake()
    {
        if (!LoadAtlases())
            return;

        foreach (var image in GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null)
                continue;

            for (var column = 0; column < SpriteNames.Length; column++)
            {
                if (image.sprite.name != SpriteNames[column])
                    continue;

                AddAnimationView(image, column);
                break;
            }
        }
    }

    private void OnEnable()
    {
        DisplayFrame(CurrentFrame());
    }

    private void LateUpdate()
    {
        var frame = CurrentFrame();
        if (frame != displayedFrame)
            DisplayFrame(frame);

        // Keep each button's existing hover and pressed tint.
        for (var i = 0; i < buttonViews.Count; i++)
        {
            var view = buttonViews[i];
            if (view.source != null && view.animation != null)
                view.animation.color = view.source.color;
        }
    }

    private static int CurrentFrame()
    {
        return Mathf.FloorToInt((Time.realtimeSinceStartup - startedAt) * FramesPerSecond)
               % TotalFrames;
    }

    private void AddAnimationView(Image image, int column)
    {
        var child = new GameObject("Animated Icon", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        child.transform.SetParent(image.transform, false);
        child.transform.SetAsFirstSibling();

        var rect = (RectTransform)child.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var animation = child.GetComponent<RawImage>();
        animation.raycastTarget = false;
        buttonViews.Add(new ButtonView { source = image, animation = animation, column = column });
    }

    private void DisplayFrame(int frame)
    {
        if (atlases == null || atlases.Length == 0)
            return;

        var atlasIndex = frame / FramesPerAtlas;
        var row = frame % FramesPerAtlas;
        var atlas = atlases[atlasIndex];
        var uvWidth = (float)IconSize / atlas.width;
        var uvHeight = (float)IconSize / atlas.height;

        for (var i = 0; i < buttonViews.Count; i++)
        {
            var view = buttonViews[i];
            if (view.animation == null)
                continue;
            view.animation.texture = atlas;
            view.animation.uvRect = new Rect(view.column * uvWidth,
                1f - (row + 1) * uvHeight, uvWidth, uvHeight);
        }

        displayedFrame = frame;
    }
}
