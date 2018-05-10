using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubSyncLib.Logic;

namespace SubSync.Tests
{
    [TestClass]
    public class FilterTests
    {
        // can't be assed to name these properly xD
        [TestMethod]
        public void VideoIgnoreTest_1()
        {
            var filter = new VideoIgnoreFilter(new[] { "*.mp4" });
            Assert.AreEqual(true, filter.Match(@"c:\blabla\bloblo\test.mp4"));
            Assert.AreEqual(false, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }

        [TestMethod]
        public void VideoIgnoreTest_2()
        {
            var filter = new VideoIgnoreFilter(new[] { "*.mp3" });
            Assert.AreEqual(false, filter.Match(@"c:\blabla\bloblo\test.mp4"));
            Assert.AreEqual(true, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }

        [TestMethod]
        public void VideoIgnoreTest_3()
        {
            var filter = new VideoIgnoreFilter(new[] { "baba/*.mp3" });
            Assert.AreEqual(true, filter.Match(@"c:\blabla\baba\test.mp3"));
            Assert.AreEqual(false, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }

        [TestMethod]
        public void VideoIgnoreTest_4()
        {
            var filter = new VideoIgnoreFilter(new[] { "*/*.mp3" });
            Assert.AreEqual(true, filter.Match(@"c:\blabla\baba\test.mp3"));
            Assert.AreEqual(true, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }

        [TestMethod]
        public void VideoIgnoreTest_5()
        {
            var filter = new VideoIgnoreFilter(new[] { "*/*.*" });
            Assert.AreEqual(true, filter.Match(@"c:\blabla\baba\test.mp3"));
            Assert.AreEqual(true, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }

        [TestMethod]
        public void VideoIgnoreTest_6()
        {
            var filter = new VideoIgnoreFilter(new[] { "*/*" });
            Assert.AreEqual(true, filter.Match(@"c:\blabla\baba\test.mp3"));
            Assert.AreEqual(true, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }

        [TestMethod]
        public void VideoIgnoreTest_7()
        {
            var filter = new VideoIgnoreFilter(new[] { "*" });
            Assert.AreEqual(true, filter.Match(@"c:\blabla\baba\test.mp3"));
            Assert.AreEqual(true, filter.Match(@"c:\blabla\bloblo\test.mp3"));
        }
    }
}