using System;
using System.Collections.Generic;

namespace _518byReplica
{
    [Serializable]
    public sealed class BroadcastItem
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Voice { get; set; }
        public bool Enabled { get; set; }
        public int RepeatCount { get; set; }

        public BroadcastItem()
        {
            Name = "新播音内容";
            Text = "欢迎光临，祝您购物愉快。";
            Voice = "";
            Enabled = true;
            RepeatCount = 1;
        }
    }

    [Serializable]
    public sealed class AppSettings
    {
        public List<BroadcastItem> Items { get; set; }
        public string MusicFolder { get; set; }
        public string SelectedVoice { get; set; }
        public int SpeechRate { get; set; }
        public int SpeechVolume { get; set; }

        public AppSettings()
        {
            Items = new List<BroadcastItem>();
            MusicFolder = "";
            SelectedVoice = "";
            SpeechRate = 0;
            SpeechVolume = 90;
        }
    }
}


