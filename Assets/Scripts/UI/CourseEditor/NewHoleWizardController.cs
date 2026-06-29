using DiskGolf.Core;
using DiskGolf.CourseEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.CourseEditor
{
    public sealed class NewHoleWizardController : MonoBehaviour
    {
        const string DefaultName = "My Par 3";
        const string DefaultThemeId = "temperate";
        const string BlankTemplateId = "blank_par3";
        const string RidgelineTemplateId = "ridgeline";

        [SerializeField] GameObject overlayRoot;

        [SerializeField] GameObject[] stepPanels;

        [SerializeField] TMP_InputField nameInput;

        [SerializeField] Toggle blankTemplateToggle;

        [SerializeField] Toggle straightTemplateToggle;

        [SerializeField] Toggle ridgelineTemplateToggle;

        [SerializeField] Toggle temperateThemeToggle;

        [SerializeField] Button cancelButton;

        [SerializeField] Button backButton;

        [SerializeField] Button nextButton;

        [SerializeField] Button startBuildingButton;

        int step;

        void Awake()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Close);

            if (backButton != null)
                backButton.onClick.AddListener(OnBack);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNext);

            if (startBuildingButton != null)
                startBuildingButton.onClick.AddListener(OnStartBuilding);

            EnsureWizardNavLayout();
        }

        void EnsureWizardNavLayout()
        {
            FixNavButtonLayout(cancelButton);
            FixNavButtonLayout(backButton);
            FixNavButtonLayout(nextButton);
            FixNavButtonLayout(startBuildingButton);
        }

        static void FixNavButtonLayout(Button button)
        {
            if (button == null)
                return;

            var rt = button.transform as RectTransform;
            if (rt == null)
                return;

            var layout = button.GetComponent<LayoutElement>();
            float width = layout != null && layout.preferredWidth > 0f ? layout.preferredWidth : 140f;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;
            }
        }

        public void Open()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(true);

            ResetWizard();
            EnsureWizardNavLayout();
        }

        public void Close()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(false);
        }

        public void ResetWizard()
        {
            step = 0;

            if (nameInput != null)
                nameInput.text = DefaultName;

            if (straightTemplateToggle != null)
                straightTemplateToggle.isOn = true;

            if (temperateThemeToggle != null)
                temperateThemeToggle.isOn = true;

            UpdateStepVisibility();
        }

        void OnBack()
        {
            if (step <= 0)
                return;

            step--;
            UpdateStepVisibility();
        }

        void OnNext()
        {
            if (step >= LastStepIndex)
                return;

            step++;
            UpdateStepVisibility();
        }

        void OnStartBuilding()
        {
            string displayName = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text)
                ? nameInput.text.Trim()
                : DefaultName;

            var catalog = HoleDataCatalog.Player;
            displayName = catalog.MakeUniqueDisplayName(displayName);

            string templateId = ResolveTemplateId();
            var hole = HoleDataTemplates.CreateFromTemplateId(templateId, displayName);
            hole.Name = displayName;
            hole.ThemeId = DefaultThemeId;

            catalog.Save(hole, published: false, templateSource: templateId);

            var theme = ThemePackLoader.Load(DefaultThemeId);
            CourseEditorSession.Instance.Load(hole, theme, hole.Id);
            CourseEditorNavigation.OpenEditor(hole.Id);
        }

        string ResolveTemplateId()
        {
            if (blankTemplateToggle != null && blankTemplateToggle.isOn)
                return BlankTemplateId;

            if (ridgelineTemplateToggle != null && ridgelineTemplateToggle.isOn)
                return RidgelineTemplateId;

            return HoleDataTemplates.StraightPar3Id;
        }

        int LastStepIndex => stepPanels != null && stepPanels.Length > 0 ? stepPanels.Length - 1 : 0;

        void UpdateStepVisibility()
        {
            if (stepPanels != null)
            {
                for (int i = 0; i < stepPanels.Length; i++)
                {
                    if (stepPanels[i] != null)
                        stepPanels[i].SetActive(i == step);
                }
            }

            if (backButton != null)
                backButton.gameObject.SetActive(step > 0);

            if (nextButton != null)
                nextButton.gameObject.SetActive(step < LastStepIndex);

            if (startBuildingButton != null)
                startBuildingButton.gameObject.SetActive(step == LastStepIndex);
        }
    }
}
