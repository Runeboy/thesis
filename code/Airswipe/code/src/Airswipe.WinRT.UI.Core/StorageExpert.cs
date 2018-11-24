using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Airswipe.WinRT.Core.Data
{
    public class StorageExpert
    {
        //TODO: YKW

        public static async Task<string> ReadTextFileAsync(string path)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync(path);
            return await FileIO.ReadTextAsync(file);
        }

        public static async void WriteTotextFileAsync(string fileName, string contents)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, contents);
        }

        public static void SaveSettings(string key, string contents)
        {
            ApplicationData.Current.LocalSettings.Values[key] = contents;
        }

        public static string GetSetting(string key)
        {
            object value = ApplicationData.Current.LocalSettings.Values[key];
            return (value == null)? null : value.ToString();
        }

        public static void SaveSettingsInContainer(string user, string key, string contents)
        {
            var localSetting = ApplicationData.Current.LocalSettings;

            localSetting.CreateContainer(user, ApplicationDataCreateDisposition.Always);

            if (localSetting.Containers.ContainsKey(user))
            {
                localSetting.Containers[user].Values[key] = contents;
            }
        }
    }
}
