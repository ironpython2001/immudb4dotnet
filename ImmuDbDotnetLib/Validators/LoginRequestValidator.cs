using FluentValidation;

namespace ImmuDbDotnetLib.Validators
{
    public class LoginRequestValidator : AbstractValidator<Pocos.LoginRequest>
    {
        public LoginRequestValidator()
        {
            this.RuleFor(x => x.User).NotNull().NotEmpty();
            this.RuleFor(x => x.Password).NotNull().NotEmpty();
        }
    }
}
