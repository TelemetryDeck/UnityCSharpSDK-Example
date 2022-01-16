using UnityEngine;
using UnityEngine.UI;

namespace TelemetryClient.TestApp.Scripts
{
    [RequireComponent(typeof(Text))]
    public class LogViewer : MonoBehaviour
    {
        [SerializeField, Header("Will scroll to the bottom when log message is received.")]
        private ScrollRect _scrollRect;

        private RectTransform _parent;
        private Text _text;

        private void Awake()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            _text = GetComponent<Text>();
            _parent = transform.parent.GetComponent<RectTransform>();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (_text == null) // app quitting
                return;
            _text.text += $"{condition}\n";
            _parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _text.preferredHeight);
            _scrollRect.normalizedPosition = Vector2.zero;
        }
    }
}
