using System;
using System.IO;
using Crosstales.FB;
using UnityEngine;
using UnityEngine.UI;

public static class FileBrowserManager
{
    public const string DefaultImageResourcePath = "NoImage";

    public struct ImageLoadResult
    {
        public Texture2D texture;
        public bool shouldDestroyTexture;
        public bool loadedFromPath;
    }

    private static readonly ExtensionFilter[] ImageExtensions =
    {
        new ExtensionFilter("Image", "png", "jpg", "jpeg")
    };

    private static Action<string, Texture2D> imageFileLoadedCallback;
    private static string initialDirectory = string.Empty;
    private static bool isWaitingForImageFile;

    public static string SelectedImagePath { get; private set; }

    public static void OpenImageFile(Action<string, Texture2D> imageLoadedCallback, string dialogTitle = "File Browser")
    {
        if (FileBrowser.Instance == null)
        {
            Debug.LogWarning("FileBrowser instance was not found.");
            return;
        }

        if (isWaitingForImageFile)
        {
            return;
        }

        imageFileLoadedCallback = imageLoadedCallback;
        isWaitingForImageFile = true;
        FileBrowser.Instance.OnOpenFilesComplete += HandleOpenFilesComplete;
        FileBrowser.Instance.OpenSingleFileAsync(dialogTitle, initialDirectory, string.Empty, ImageExtensions);
    }

    public static Texture2D LoadImageTexture(string filePath)
    {
        if (!IsSupportedImagePath(filePath) || !File.Exists(filePath))
        {
            Debug.LogWarning($"Unsupported or missing image file: {filePath}");
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(imageBytes))
        {
            UnityEngine.Object.Destroy(texture);
            Debug.LogWarning($"Failed to load image file: {filePath}");
            return null;
        }

        return texture;
    }

    public static ImageLoadResult LoadImageTextureOrDefault(string filePath, string defaultResourcePath = DefaultImageResourcePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            Texture2D loadedTexture = LoadImageTexture(filePath);
            if (loadedTexture != null)
            {
                return new ImageLoadResult
                {
                    texture = loadedTexture,
                    shouldDestroyTexture = true,
                    loadedFromPath = true
                };
            }
        }

        return new ImageLoadResult
        {
            texture = LoadDefaultImageTexture(defaultResourcePath),
            shouldDestroyTexture = false,
            loadedFromPath = false
        };
    }

    public static Texture2D LoadDefaultImageTexture(string resourcePath = DefaultImageResourcePath)
    {
        Texture2D defaultTexture = Resources.Load<Texture2D>(resourcePath);
        if (defaultTexture == null)
        {
            Debug.LogWarning($"Default image was not found in Resources/{resourcePath}.");
        }

        return defaultTexture;
    }

    public static void ApplyTextureToRawImage(
        RawImage targetRawImage,
        Texture texture,
        ref AspectRatioFitter aspectRatioFitter,
        bool preserveAspect = true,
        AspectRatioFitter.AspectMode aspectMode = AspectRatioFitter.AspectMode.FitInParent)
    {
        if (targetRawImage == null)
        {
            return;
        }

        targetRawImage.texture = texture;
        targetRawImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        if (aspectRatioFitter == null)
        {
            aspectRatioFitter = targetRawImage.GetComponent<AspectRatioFitter>();
        }

        if (!preserveAspect || texture == null || texture.height <= 0)
        {
            if (aspectRatioFitter != null)
            {
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.None;
                aspectRatioFitter.enabled = false;
            }

            return;
        }

        if (aspectRatioFitter == null)
        {
            aspectRatioFitter = targetRawImage.gameObject.AddComponent<AspectRatioFitter>();
        }

        aspectRatioFitter.enabled = true;
        aspectRatioFitter.aspectMode = aspectMode;
        aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
    }

    public static void DestroyTextureIfNeeded(Texture2D texture, bool shouldDestroyTexture)
    {
        if (shouldDestroyTexture && texture != null)
        {
            UnityEngine.Object.Destroy(texture);
        }
    }

    private static void HandleOpenFilesComplete(bool selected, string singleFile, string[] files)
    {
        if (FileBrowser.Instance != null)
        {
            FileBrowser.Instance.OnOpenFilesComplete -= HandleOpenFilesComplete;
        }

        if (!isWaitingForImageFile)
        {
            return;
        }

        isWaitingForImageFile = false;

        if (!selected || string.IsNullOrWhiteSpace(singleFile))
        {
            return;
        }

        Texture2D texture = LoadImageTexture(singleFile);
        if (texture == null)
        {
            return;
        }

        SelectedImagePath = singleFile;
        initialDirectory = Path.GetDirectoryName(singleFile);
        imageFileLoadedCallback?.Invoke(singleFile, texture);
        imageFileLoadedCallback = null;
    }

    private static bool IsSupportedImagePath(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
    }
}
