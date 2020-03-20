using Vipps.Helpers;
using Xunit;
using Assert = Xunit.Assert;

namespace Vipps.Test.Tests
{
    public class PhoneNumberTests
    {
        [Fact]
        public void GetPhoneNumberWhenWrongCountryCode_ShouldReturnEmptyString()
        {
            var phoneNumber = "+46 123 12 123";
            phoneNumber = PhoneNumberHelper.Validate(phoneNumber);

            Assert.Empty(phoneNumber);
        }

        [Fact]
        public void GetPhoneNumberWhenCountryCode_ShouldReturnPhoneNumber()
        {
            var phoneNumber = "+47 123 12 123";
            var expected = "12312123";
            phoneNumber = PhoneNumberHelper.Validate(phoneNumber);

            Assert.Equal(expected, phoneNumber);
        }

        [Fact]
        public void GetPhoneNumberWhenCountryCodeStartsWith00_ShouldReturnPhoneNumber()
        {
            var phoneNumber = "0047 123 12 123";
            var expected = "12312123";
            phoneNumber = PhoneNumberHelper.Validate(phoneNumber);

            Assert.Equal(expected, phoneNumber);
        }

        [Fact]
        public void GetPhoneNumberWhenCorrect_ShouldReturnPhoneNumber()
        {
            var phoneNumber = "12312123";
            var expected = "12312123";
            phoneNumber = PhoneNumberHelper.Validate(phoneNumber);

            Assert.Equal(expected, phoneNumber);
        }

        [Fact]
        public void GetPhoneNumberWhenNull_ShouldReturnEmptyString()
        {
            string phoneNumber = null;

            phoneNumber = PhoneNumberHelper.Validate(phoneNumber);

            Assert.Empty(phoneNumber);
        }

        [Fact]
        public void GetPhoneNumberWhenNotValid_ShouldReturnEmptyString()
        {
            string phoneNumber = "0761234567";

            phoneNumber = PhoneNumberHelper.Validate(phoneNumber);

            Assert.Empty(phoneNumber);
        }
    }
}
