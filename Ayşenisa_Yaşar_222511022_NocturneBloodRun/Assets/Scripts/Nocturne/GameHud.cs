using UnityEngine;
using UnityEngine.UI;

namespace Nocturne
{
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private Text mainText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private GameObject statusPanel;

        public void Configure(
            Text main,
            Text status,
            Button start,
            Button restart,
            GameObject startRoot,
            GameObject infoRoot,
            GameObject statusRoot)
        {
            mainText = main;
            statusText = status;
            startButton = start;
            restartButton = restart;
            startPanel = startRoot;
            infoPanel = infoRoot;
            statusPanel = statusRoot;
        }

        public void SetMainText(string text)
        {
            if (mainText != null)
            {
                mainText.text = text;
            }
        }

        public void SetStatusText(string text, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = text;
            statusText.color = color;
            statusText.enabled = !string.IsNullOrWhiteSpace(text);
            if (statusPanel != null)
            {
                statusPanel.SetActive(statusText.enabled);
            }
        }

        public void BindButtons(System.Action startAction, System.Action restartAction)
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => startAction?.Invoke());
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => restartAction?.Invoke());
            }
        }

        public void ShowStartPanel(bool visible)
        {
            if (startPanel != null)
            {
                startPanel.SetActive(visible);
            }
        }

        public void ShowRestartButton(bool visible)
        {
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(visible);
            }
        }

        public void SetStartButtonLabel(string text)
        {
            if (startButton == null)
            {
                return;
            }

            Text label = startButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        public void SetRestartButtonLabel(string text)
        {
            if (restartButton == null)
            {
                return;
            }

            Text label = restartButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        public void SetStartMessage(string text)
        {
            if (startPanel == null)
            {
                return;
            }

            Text[] texts = startPanel.GetComponentsInChildren<Text>(true);
            foreach (Text item in texts)
            {
                if (item.gameObject.name == "StartMessage")
                {
                    item.text = text;
                    break;
                }
            }
        }

        public void ShowInfoPanel(bool visible)
        {
            if (infoPanel != null)
            {
                infoPanel.SetActive(visible);
            }
        }
    }
}
