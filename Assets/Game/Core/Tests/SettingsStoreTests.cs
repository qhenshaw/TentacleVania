using NUnit.Framework;
using TentacleVania.Core.Settings;

namespace TentacleVania.Core.Tests
{
    public sealed class SettingsStoreTests
    {
        [Test]
        public void RoundTripsEveryField()
        {
            var data = new SettingsData { MusicVolume = 0.3f, Gore = false, Colorblind = ColorblindMode.Tritanopia, DisplayMode = DisplayModeSetting.Windowed, Width = 1280, Height = 720, FrameCap = 60, Language = "fr", Bindings = "{}" };
            var back = SettingsStore.Parse(SettingsStore.Serialize(data));
            Assert.That(back.MusicVolume, Is.EqualTo(0.3f));
            Assert.That(back.Gore, Is.False);
            Assert.That(back.Colorblind, Is.EqualTo(ColorblindMode.Tritanopia));
            Assert.That(back.DisplayMode, Is.EqualTo(DisplayModeSetting.Windowed));
            Assert.That(back.Width, Is.EqualTo(1280));
            Assert.That(back.Language, Is.EqualTo("fr"));
            Assert.That(back.Bindings, Is.EqualTo("{}"));
        }

        [Test]
        public void BadOrMissingFilesGiveDefaults()
        {
            Assert.That(SettingsStore.Parse("").MusicVolume, Is.EqualTo(new SettingsData().MusicVolume));
            Assert.That(SettingsStore.Parse("not json").Gore, Is.True);
        }

        [Test]
        public void OutOfRangeValuesAreClamped()
        {
            var back = SettingsStore.Parse("{\"MasterVolume\": 4, \"EnemySpeedScale\": 0.1, \"DamageTakenScale\": 9}");
            Assert.That(back.MasterVolume, Is.EqualTo(1f));
            Assert.That(back.EnemySpeedScale, Is.EqualTo(0.5f));
            Assert.That(back.DamageTakenScale, Is.EqualTo(2f));
        }
    }
}
