using System.ComponentModel.Composition;
using System.Configuration;

namespace OuiGui.WPF.Services
{
    [Export(typeof(IConfigurationService))]
    public class ConfigurationService : IConfigurationService
    {
        private Configuration configuration;

        [ImportingConstructor]
        public ConfigurationService()
        {
            this.configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        public string GetAppSetting(string key)
        {
            return this.configuration.AppSettings.Settings[key].Value;
        }

        public void SetAppSetting(string key, string value)
        {
            this.configuration.AppSettings.Settings[key].Value = value;
        }

        public void Save()
        {
            this.configuration.Save(ConfigurationSaveMode.Modified);
        }
    }
}
