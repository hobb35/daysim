using UnityEngine;
using UnityEngine.UI;

namespace DaySim.UI
{
    /// <summary>
    /// High-level UI wiring component for the DaySim scene.
    /// Keeps references to core widgets so the scene is easy to set up.
    /// Panels: Dashboard | History | Quests | Achievements | Stats
    /// </summary>
    public class DaySimUIRoot : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private DaySimManager daySimManager;

        [Header("Input")]
        [SerializeField] private InputField actionInputField;
        [SerializeField] private Button submitButton;

        [Header("Panels")]
        [SerializeField] private GameObject dashboardPanel;
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private GameObject questsPanel;
        [SerializeField] private GameObject achievementsPanel;
        [SerializeField] private GameObject statsPanel;

        [Header("Navigation Buttons")]
        [SerializeField] private Button dashboardTabButton;
        [SerializeField] private Button historyTabButton;
        [SerializeField] private Button questsTabButton;
        [SerializeField] private Button achievementsTabButton;
        [SerializeField] private Button statsTabButton;

        private void Awake()
        {
            if (daySimManager == null)
                daySimManager = FindObjectOfType<DaySimManager>();

            if (actionInputField != null && daySimManager != null)
                daySimManager.SetActionInputField(actionInputField);

            if (submitButton != null)
                submitButton.onClick.AddListener(() => daySimManager?.SubmitCurrentInput());

            WireTab(dashboardTabButton,    dashboardPanel);
            WireTab(historyTabButton,      historyPanel);
            WireTab(questsTabButton,       questsPanel);
            WireTab(achievementsTabButton, achievementsPanel);
            WireTab(statsTabButton,        statsPanel);
        }

        private void Start()
        {
            // Default to dashboard on load.
            ShowPanel(dashboardPanel);
        }

        private void WireTab(Button button, GameObject panel)
        {
            if (button != null)
                button.onClick.AddListener(() => ShowPanel(panel));
        }

        private void ShowPanel(GameObject panelToShow)
        {
            SetActive(dashboardPanel,    panelToShow == dashboardPanel);
            SetActive(historyPanel,      panelToShow == historyPanel);
            SetActive(questsPanel,       panelToShow == questsPanel);
            SetActive(achievementsPanel, panelToShow == achievementsPanel);
            SetActive(statsPanel,        panelToShow == statsPanel);
        }

        private static void SetActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
