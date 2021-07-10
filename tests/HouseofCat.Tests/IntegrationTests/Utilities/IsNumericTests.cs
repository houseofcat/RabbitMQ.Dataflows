#nullable enable

using HouseofCat.Extensions;
using System;
using Xunit;

namespace HouseofCat.Tests.IntegrationTests
{
    public class IsNumericTests
    {
        [Fact]
        public void IsNumeric()
        {
            var value = 0;

            Assert.True(value.IsNumeric());
        }

        [Fact]
        public void IsObjectNumeric()
        {
            var value = 0;
            var objValue = (object)value;

            Assert.False(objValue.IsNumeric());
        }

        [Fact]
        public void IsNumericAtRuntime()
        {
            var value = 0;

            Assert.True(value.IsNumericAtRuntime());
        }

        [Fact]
        public void IsObjectNumericAtRuntime()
        {
            var value = 0;
            var objValue = (object)value;

            Assert.True(objValue.IsNumericAtRuntime());
        }

        [Fact]
        public void IsNullableNumeric()
        {
            int? value = 0;

            Assert.True(value.IsNullableNumeric());
        }

        [Fact]
        public void IsNullableNumericAsObject()
        {
            int? value = 0;
            var objValue = (object)value;

            Assert.True(objValue.IsNullableNumeric());
        }

        [Fact]
        public void IsNullableNumericWhenNull()
        {
            int? value = null;

            Assert.True(value.IsNullableNumeric());
        }

        [Fact]
        public void IsNullableNumericWhenNullAsObject()
        {
            int? value = null;
            var objValue = value as object;

            Assert.False(objValue.IsNullableNumeric());
        }

        [Fact]
        public void IsNullableNumericWhenNotNumber()
        {
            string value = "test";

            Assert.False(value.IsNullableNumeric());
        }

        [Fact]
        public void IsNullableNumericWhenNullAndNotNumber()
        {
            Type? value = null;

            Assert.False(value.IsNullableNumeric());
        }
    }
}
