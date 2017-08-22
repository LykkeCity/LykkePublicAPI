using System.IO;

namespace AzureRepositories
{
    public static class GeneralSettingsReader
    {
        public static T ReadGeneralSettingsLocal<T>(string path)
        {
            var content = File.ReadAllText(path);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }
    }
}
