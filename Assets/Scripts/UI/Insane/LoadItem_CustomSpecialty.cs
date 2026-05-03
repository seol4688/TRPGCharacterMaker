using System;
using UnityEngine;
using UnityEngine.UI;

public class LoadItem_CustomSpecialty : LoadItem
{
    [SerializeField] private Toggle favoriteToggle;

    public bool IsFavorite => favoriteToggle != null && favoriteToggle.isOn;

    public void Setup(string fileName, bool isFavorite)
    {
        if (nameText != null)
            nameText.text = fileName ?? string.Empty;

        if (favoriteToggle != null)
            favoriteToggle.SetIsOnWithoutNotify(isFavorite);
    }

    public void SetFavoriteCallback(Action<bool> onFavoriteChanged)
    {
        if (favoriteToggle == null)
            return;

        favoriteToggle.onValueChanged.RemoveAllListeners();
        favoriteToggle.onValueChanged.AddListener(val => onFavoriteChanged?.Invoke(val));
    }
}
