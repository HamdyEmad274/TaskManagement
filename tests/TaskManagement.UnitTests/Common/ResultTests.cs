using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Common;

namespace TaskManagement.UnitTests.Common
{
    public class ResultTests
    {
        [Fact]
        public void Success_ShouldSetIsSuccessTrue_AndEmptyError()
        {
            var result = Result.Success("Testingg");

            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().BeEmpty();
            result.Value.Should().Be("Testingg");
        }
        [Fact]
        public void Failure_ShouldSetIsSuccessFalse_AndKeepErrorMessage()
        {
            var result = Result.Failure<string>("Testingg");

            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Testingg");
        }
        [Fact]
        public void Value_WhenFailure_ShouldThrowInvalidOperationException()
        {
            var result = Result.Failure<string>("Testingg");

            var act = () => { var val = result.Value; };
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot access the value of a failed result.");;

        }
    }
}
