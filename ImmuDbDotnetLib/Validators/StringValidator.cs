using FluentValidation;

namespace ImmuDbDotnetLib.Validators
{
    public class StringValidator : AbstractValidator<string>
    {
        public StringValidator()
        {
            this.RuleFor(x => x).NotNull().NotEmpty();
        }
    }
}
