using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Behaviours;
using TaskManagement.Application.Users.Commands.RegisterUser;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;

namespace TaskManagement.UnitTests.Common
{
    public class ValidationBehaviourTests
    {
        [Fact]
        public async Task Handle_WhenValidationFails_ShouldThrowValidationException_AndNeverCallsNext()
        {
            var validators = new List<IValidator<RegisterUserCommand>> 
            { 
                new RegisterUserCommandValidator() 
            };
            var behaviour = new ValidationBehavior<RegisterUserCommand, Result<Guid>>(validators);
            var request = new RegisterUserCommand("John Doe", "invalidemail", "password");

            var mockNext = new Mock<RequestHandlerDelegate<Result<Guid>>>();

            var act = async()=> await behaviour.Handle(request, mockNext.Object, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            mockNext.Verify(next=> next(), Times.Never());
            

        }
    }
}
