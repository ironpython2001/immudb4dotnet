using FluentValidation;

namespace ImmuDbDotnetLib.Validators
{
    public class IntValidator : AbstractValidator<int>
    {
        public IntValidator()
        {
            this.RuleFor(x => x).Must((x) => x > 0);
        }
    }
}
