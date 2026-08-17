using System.Windows;

namespace EliteFIPServer.Infrastructure
{
    /// <summary>
    /// Manages application theme switching (Light/Dark)
    /// </summary>
    public class ThemeManager
    {
        private ResourceDictionary _currentTheme;
        private bool _isDarkMode = false;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    ApplyTheme();
                }
            }
        }

        public ThemeManager()
        {
            // Initialize with light theme
            _isDarkMode = false;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var app = Application.Current;
            var themeDictionary = GetThemeDictionary();

            // Remove old theme
            if (_currentTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(_currentTheme);
            }

            // Add new theme
            if (themeDictionary != null)
            {
                app.Resources.MergedDictionaries.Add(themeDictionary);
                _currentTheme = themeDictionary;
            }
        }

        private ResourceDictionary GetThemeDictionary()
        {
            string themeName = _isDarkMode ? "DarkTheme" : "LightTheme";
            var uri = new Uri($"Infrastructure/Themes/{themeName}.xaml", UriKind.Relative);
            return new ResourceDictionary { Source = uri };
        }

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }
    }
}
