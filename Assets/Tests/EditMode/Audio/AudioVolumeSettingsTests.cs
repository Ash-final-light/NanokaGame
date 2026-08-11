using NanokaGame.Audio;
using NUnit.Framework;

namespace NanokaGame.Tests.EditMode.Audio
{
    public sealed class AudioVolumeSettingsTests
    {
        [Test]
        public void LinearToDecibels_WhenValueChanges_ReturnsSafeExpectedValue()
        {
            Assert.That(AudioVolumeSettings.LinearToDecibels(0f), Is.EqualTo(-80f));
            Assert.That(AudioVolumeSettings.LinearToDecibels(1f), Is.EqualTo(0f));
            Assert.That(
                AudioVolumeSettings.LinearToDecibels(0.5f),
                Is.EqualTo(-6.0206f).Within(0.0001f));

            float minimumDecibels = AudioVolumeSettings.LinearToDecibels(0f);
            Assert.That(float.IsNaN(minimumDecibels), Is.False);
            Assert.That(float.IsInfinity(minimumDecibels), Is.False);
        }
    }
}
