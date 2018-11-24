using Airswipe.WinRT.Core.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Airswipe.WinRT.Core.Test
{
    [TestClass]
    public class MathExpertTest : MathExpert
    {
        [TestMethod]
        public void TestMedianOdd1()
        {
            var testValues = new double[] { 7.3290045, 4.8341918, 8.7776885, 4.9590869, };
            var median = GetMedian(testValues);
            Assert.AreEqual(median, 6.1440456999999995);
        }

        [TestMethod]
        public void TestMedianOdd2()
        {
            var testValues = new double[] { 30, 171, 184, 201, 212, 250, 265, 270, 272, 289, 305, 306, 322, 322, 336, 346, 351, 370, 390, 404, 409, 411, 436, 437, 439, 441, 444, 448, 451, 453, 470, 480, 482, 487, 494, 495, 499, 503, 514, 521, 522, 527, 548, 550, 559, 560, 570, 572, 574, 578, 585, 592, 592, 607, 616, 618, 621, 629, 637, 638, 640, 656, 668, 707, 709, 719, 737, 739, 752, 758, 766, 792, 792, 794, 802, 818, 830, 832, 843, 858, 860, 869, 918, 925, 953, 991, 1000, 1005, 1068, 1441 };
            var median = GetMedian(testValues);
            Assert.AreEqual(median, 559.5);
        }

        [TestMethod]
        public void TestMedianEqual()
        {
            var testValues = new double[] { 3, 5, 7, 12, 13, 14, 21, 23, 23, 23, 23, 29, 39, 40, 56 };
            var median = GetMedian(testValues);
            Assert.AreEqual(median, 23);
        }

        [TestMethod]
        public void TestLowerQuartile()
        {
            var testValues = new double[] { 30, 171, 184, 201, 212, 250, 265, 270, 272, 289, 305, 306, 322, 322, 336, 346, 351, 370, 390, 404, 409, 411, 436, 437, 439, 441, 444, 448, 451, 453, 470, 480, 482, 487, 494, 495, 499, 503, 514, 521, 522, 527, 548, 550, 559, 560, 570, 572, 574, 578, 585, 592, 592, 607, 616, 618, 621, 629, 637, 638, 640, 656, 668, 707, 709, 719, 737, 739, 752, 758, 766, 792, 792, 794, 802, 818, 830, 832, 843, 858, 860, 869, 918, 925, 953, 991, 1000, 1005, 1068, 1441 };

            var lq = GetLowerQuartile(testValues);

            Assert.AreEqual(lq, 429.75);
        }

        [TestMethod]
        public void TestUpperQuartile()
        {
            var testValues = new double[] { 30, 171, 184, 201, 212, 250, 265, 270, 272, 289, 305, 306, 322, 322, 336, 346, 351, 370, 390, 404, 409, 411, 436, 437, 439, 441, 444, 448, 451, 453, 470, 480, 482, 487, 494, 495, 499, 503, 514, 521, 522, 527, 548, 550, 559, 560, 570, 572, 574, 578, 585, 592, 592, 607, 616, 618, 621, 629, 637, 638, 640, 656, 668, 707, 709, 719, 737, 739, 752, 758, 766, 792, 792, 794, 802, 818, 830, 832, 843, 858, 860, 869, 918, 925, 953, 991, 1000, 1005, 1068, 1441 };

            var uq = GetUpperQuartile(testValues);

            Assert.AreEqual(uq, 742.25);
        }

    }
}
