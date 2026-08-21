using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Application.Users.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Username is required")
                .MaximumLength(100);
            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("email address is required")
                .EmailAddress().WithMessage("Invalid email address");

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$")
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number");
        }
    }
}
