using FluentValidation;
using STak.TakHub.Models.Request;

namespace STak.TakHub.Models.Validation
{
    public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
    {
        public RegisterUserRequestValidator()
        {
            RuleFor(x => x.Email).EmailAddress();

#if DEBUG
            RuleFor(x => x.FirstName).Length(1, 63);
            RuleFor(x => x.LastName).Length(1, 63);
            RuleFor(x => x.UserName).Length(1, 255);
            RuleFor(x => x.Password).Length(1, 63);
#else
            RuleFor(x => x.FirstName).Length(2, 63);
            RuleFor(x => x.LastName).Length(2, 63);
            RuleFor(x => x.UserName).Length(3, 255);
            RuleFor(x => x.Password).Length(6, 63);
#endif
        }
    }
}
