using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Dark;

public class LoadItem_Character : LoadItem
{
    [SerializeField] private RawImage avatar;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Texture2D loadedTexture;
    private bool shouldDestroyTexture;

    private void OnDestroy()
    {
        FileBrowserManager.DestroyTextureIfNeeded(loadedTexture, shouldDestroyTexture);
        loadedTexture = null;
    }

    public void Setup(string characterName, string ruleName, string avatarImagePath, System.DateTime? creationDate = null)
    {
        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(characterName) ? "미정" : characterName;

        if (descriptionText != null)
        {
            string dateStr = creationDate.HasValue
                ? creationDate.Value.ToString("yyyy.MM.dd")
                : "****.**.**";
            descriptionText.text = $"룰 - {ruleName ?? string.Empty} / 생성날짜 - {dateStr}";
        }

        if (avatar != null)
        {
            FileBrowserManager.ImageLoadResult result = FileBrowserManager.LoadImageTextureOrDefault(avatarImagePath);
            loadedTexture = result.texture;
            shouldDestroyTexture = result.shouldDestroyTexture;
            AspectRatioFitter fitter = null;
            FileBrowserManager.ApplyTextureToRawImage(avatar, loadedTexture, ref fitter);
        }
    }
}
