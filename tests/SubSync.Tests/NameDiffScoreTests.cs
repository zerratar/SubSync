using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SubSync.Tests
{
    [TestClass]
    public class NameDiffScoreTests
    {
        [TestMethod]
        public void Test1()
        {
            var score0 = FilenameDiff.GetDiffScore("Greys Anatomy s11e11.mp4",
                "Greys.Anatomy.S09.720p.HDTV.X264-DIMENSION.mp4");

            Assert.AreEqual(15.6, score0);

            var score1 = FilenameDiff.GetDiffScore("Greys.Anatomy.S09.480p.HDTV.x264-mSD.mp4",
                "Greys.Anatomy.S09.720p.HDTV.X264-DIMENSION.mp4");

            Assert.AreEqual(6.5, score1);

            var score2 = FilenameDiff.GetDiffScore("Greys.Anatomy.S09.480p.HDTV.x264-mSD.mp4",
                "Greys.Anatomy.S09.480p.HDTV.x264-mSD.mp4");

            Assert.AreEqual(0, score2);
        }
    }
}