using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>Colours a Graphic (Image or text) from a theme token.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class ThemedGraphic : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private ThemeColor token = ThemeColor.White;

        private Graphic _graphic;

        public UiTheme Theme
        {
            get => theme;
            set { theme = value; Apply(); }
        }

        public ThemeColor Token
        {
            get => token;
            set { token = value; Apply(); }
        }

        public Graphic Graphic => _graphic != null ? _graphic : _graphic = GetComponent<Graphic>();

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            if (theme == null || Graphic == null)
                return;

            Graphic.color = theme.Get(token);
        }
    }
}
