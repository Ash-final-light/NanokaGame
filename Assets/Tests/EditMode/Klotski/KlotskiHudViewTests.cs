using NanokaGame.Games.Klotski.UI;
using NUnit.Framework;

namespace NanokaGame.Tests.EditMode.Klotski
{
    public sealed class KlotskiHudViewTests
    {
        [TestCase(0d, "00:00")]
        [TestCase(65.9d, "01:05")]
        [TestCase(3599d, "59:59")]
        [TestCase(3600d, "01:00:00")]
        [TestCase(3661d, "01:01:01")]
        public void FormatElapsedTime_WhenDurationChanges_ReturnsExpectedText(
            double elapsedTimeSeconds,
            string expected)
        {
            Assert.That(KlotskiHudView.FormatElapsedTime(elapsedTimeSeconds), Is.EqualTo(expected));
        }
    }
}
