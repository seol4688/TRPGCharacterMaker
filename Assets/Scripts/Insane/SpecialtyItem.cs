using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SpecialtyItem : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    private struct StateVisual
    {
        public Color imageColor;
        public Color textColor;
    }

    public enum SkillState
    {
        Normal,
        Fear
    }

    public const int MinDifficulty = 5;
    public const int MaxDifficulty = 12;

    [SerializeField] private Toggle checkbox;
    [SerializeField] private Image stateImage;
    [SerializeField] private string specialtyName;
    [SerializeField] private TextMeshProUGUI specialtyName_text;
    [SerializeField] private TextMeshProUGUI difficulty_text;
    [SerializeField] private int difficulty = MaxDifficulty;
    [SerializeField] private SkillState state = SkillState.Normal;
    [SerializeField] private StateVisual normalVisual = new StateVisual { imageColor = Color.white, textColor = Color.black };
    [SerializeField] private StateVisual fearVisual = new StateVisual { imageColor = Color.white, textColor = Color.black };

    private Action<SpecialtyItem, bool> onCheckedChanged;
    private Action<SpecialtyItem> onRightClicked;

    public int Difficulty => difficulty;
    public bool IsChecked => checkbox != null && checkbox.isOn;
    public string SkillName => specialtyName;
    public SkillState State => state;

    private void Awake()
    {
        difficulty = Mathf.Clamp(difficulty, MinDifficulty, MaxDifficulty);

        if (checkbox != null)
        {
            checkbox.interactable = false;
        }

        RefreshUI();
    }

    public void Initialize(Action<SpecialtyItem, bool> checkedChangedCallback, Action<SpecialtyItem> rightClickedCallback = null)
    {
        onCheckedChanged = checkedChangedCallback;
        onRightClicked = rightClickedCallback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (checkbox != null)
            {
                checkbox.SetIsOnWithoutNotify(!checkbox.isOn);
            }

            onCheckedChanged?.Invoke(this, IsChecked);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            onRightClicked?.Invoke(this);
        }
    }

    public void SetDifficulty(int newCount)
    {
        difficulty = Mathf.Clamp(newCount, MinDifficulty, MaxDifficulty);
        RefreshUI();
    }

    public void SetSkillName(string newSkillName)
    {
        specialtyName = newSkillName ?? string.Empty;
        RefreshUI();
    }

    public void SetState(SkillState newState)
    {
        state = newState;
        RefreshUI();
    }

    public void SetCheckedSilently(bool isChecked)
    {
        if (checkbox == null)
        {
            return;
        }

        checkbox.SetIsOnWithoutNotify(isChecked);
    }

    private void OnValidate()
    {
        difficulty = Mathf.Clamp(difficulty, MinDifficulty, MaxDifficulty);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (specialtyName_text != null)
        {
            specialtyName_text.text = specialtyName;
        }

        if (difficulty_text != null)
        {
            difficulty_text.text = difficulty.ToString();
        }

        if (stateImage != null)
        {
            stateImage.color = GetImageColorForState();
        }

        Color textColor = GetVisualForState().textColor;

        if (specialtyName_text != null)
        {
            specialtyName_text.color = textColor;
        }

        if (difficulty_text != null)
        {
            difficulty_text.color = textColor;
        }
    }

    private Color GetImageColorForState()
    {
        return GetVisualForState().imageColor;
    }

    private StateVisual GetVisualForState()
    {
        switch (state)
        {
            case SkillState.Fear:
                return fearVisual;
            default:
                return normalVisual;
        }
    }
}
