using HouseofCat.Utilities.Random;
using Xunit;

namespace HouseofCat.IntegrationTests
{
    public class UtilitiesTests
    {
        [Fact]
        public void CreateRandomBytes()
        {
            var xorShift = new XorShift();

            var bytes0 = new byte[1000];
            var bytes1 = new byte[1000];
            var bytes2 = new byte[1000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);
            xorShift.FillBuffer(bytes2, 0, bytes2.Length);

            Assert.NotEqual(bytes0, bytes1);
            Assert.NotEqual(bytes0, bytes2);
            Assert.NotEqual(bytes1, bytes2);
        }

        [Fact]
        public void CreateHundredRandomBytes()
        {
            var xorShift = new XorShift(true);

            var bytes0 = new byte[100];
            var bytes1 = new byte[100];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateFiveHundredRandomBytes()
        {
            var xorShift = new XorShift(true);

            var bytes0 = new byte[500];
            var bytes1 = new byte[500];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateThousandRandomBytes()
        {
            var xorShift = new XorShift(true);

            var bytes0 = new byte[1_000];
            var bytes1 = new byte[1_000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateTenThousandRandomBytes()
        {
            var xorShift = new XorShift(true);

            var bytes0 = new byte[10_000];
            var bytes1 = new byte[10_000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateHundredThousandRandomBytes()
        {
            var xorShift = new XorShift(true);

            var bytes0 = new byte[100_000];
            var bytes1 = new byte[100_000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }
    }
}
