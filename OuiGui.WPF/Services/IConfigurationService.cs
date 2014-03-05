using System;

namespace OuiGui.WPF.Services
{
    public interface IConfigurationService
    {
        string GetAppSetting(string key);
        void SetAppSetting(string key, string value);
        void Save();
    }
}
