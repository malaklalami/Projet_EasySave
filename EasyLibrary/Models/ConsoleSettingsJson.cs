//ConsoleSettingsJson.cs
using System;
namespace EasyLibrary.Models
{
	public class ConsoleSettingsJson
	{
        public string Language { get; set; } = "fr";
        public List<string> EncryptionExtensions { get; set; } = new List<string>();
        public string LogFormat { get; set; } = "json";
        public string BusinessSoftware { get; set; } = "Calculator";
    }
}

