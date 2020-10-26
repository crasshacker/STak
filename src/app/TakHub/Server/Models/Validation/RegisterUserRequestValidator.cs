using FluentValidation;
using STak.TakHub.Models.Request;

namespace STak.TakHub.Models.Validation
{
    public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
    {
        public RegisterUserRequestValidator()
        {
            RuleFor(x => x.FirstName).Length(2, 63);
            RuleFor(x => x.LastName).Length(2, 63);
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.UserName).Length(3, 255);
            RuleFor(x => x.Password).Length(6, 63);
        }
    }
}
