using ChedVX.UI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Configuration
{
    internal sealed class SoundSettings : SettingsBase
    {
        public static SoundSettings Default { get; } = (SoundSettings)Synchronized(new SoundSettings());

        private SoundSettings()
        {
        }

        // ref: https://stackoverflow.com/a/12807699
        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")] // empty dictionary
        public Dictionary<string, SoundSource> ScoreSound
        {
            get => (Dictionary<string, SoundSource>)this["ScoreSound"];
            set => this["ScoreSound"] = value;
        }

        [UserScopedSetting]
        public SoundSource GuideSound
        {
            get => (SoundSource)this["GuideSound"] ?? new SoundSource("guide.wav", 0.036);
            set => this["GuideSound"] = value;
        }
    }
}
