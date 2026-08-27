using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Users.Commands.RegisterUser;

namespace TaskManagement.UnitTests.UserTests
{
    public class RegisterUserCommandValidatorTests
    {
        [Fact]
        public void Validate_WhenEmailIsInvalid_ShouldReturnError()
        {
            var validator = new RegisterUserCommandValidator();
            var cmd = new RegisterUserCommand("John Doe", "invalidemail", "Password@123");
            var result = validator.TestValidate(cmd);

            result.ShouldHaveValidationErrorFor(c => c.Email);
        }
        [Fact]
        public void Validate_WhenEmailIsEmpty_ShouldReturnError()
        {
            var validator = new RegisterUserCommandValidator();
            var cmd = new RegisterUserCommand("John Doe", "", "Password@123");
            var result = validator.TestValidate(cmd);

            result.ShouldHaveValidationErrorFor(c => c.Email);

        }
        [Theory]
        [InlineData("John Doe", "johndoe@gmail.com", "Password")] // does not contain number
        [InlineData("John Doe", "johndoe@gmail.com", "password@12")] // does not contain uppercase letter
        [InlineData("John Doe", "johndoe@gmail.com", "Pa1@")] // too short
        public void Validate_WhenPasswordIsInvalid_ShouldReturnError(string name, string email, string password)
        {
            var validator = new RegisterUserCommandValidator();
            var cmd = new RegisterUserCommand(name, email, password);
            var result = validator.TestValidate(cmd);

            result.ShouldHaveValidationErrorFor(c => c.Password);
        }
        [Fact]
        public void Validate_WhenPasswordIsEmpty_ShouldReturnError()
        {
            var validator = new RegisterUserCommandValidator();
            var cmd = new RegisterUserCommand("John Doe", "johndoe@gmail.com", "");
            var result = validator.TestValidate(cmd);

            result.ShouldHaveValidationErrorFor(c => c.Password);
        }

        [Fact]
        public void Validate_WhenEmailIsEmpty_ShouldOnlyTriggerNotEmptyError_AndStopCascade()
        {
            var validator = new RegisterUserCommandValidator();
            var cmd = new RegisterUserCommand("John Doe", "", "Password@123");
            var result = validator.TestValidate(cmd);

            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("email address is required")
                .Only();
        }

        [Fact]
        public void Validate_WhenValid_ShouldNotReturnError()
        {
            var validator = new RegisterUserCommandValidator();
            var cmd = new RegisterUserCommand("John Doe", "johndoe@gmail.com", "Password@123");
            var result = validator.TestValidate(cmd);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
