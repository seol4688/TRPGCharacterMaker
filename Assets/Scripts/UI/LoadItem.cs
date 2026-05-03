using Michsky.UI.Dark;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadItem : MonoBehaviour
{
    [SerializeField] protected UIElementSound elementSound;

    [SerializeField] protected Button button;
    [SerializeField] protected Button deleteButton;

    [SerializeField] protected TextMeshProUGUI nameText;

    public void SetAudioSource(AudioSource audioSource)
    {
        if (elementSound != null)
            elementSound.audioSource = audioSource;
    }

    public void SetCallback(Action onClicked)
    {
        Button btn = button != null ? button : GetComponent<Button>();
        if (btn == null)
            return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClicked?.Invoke());
    }

    public void SetDeleteCallback(Action onDelete)
    {
        if (deleteButton == null)
            return;

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => onDelete?.Invoke());
    }
}
