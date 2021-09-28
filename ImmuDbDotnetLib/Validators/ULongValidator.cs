using FluentValidation;

namespace ImmuDbDotnetLib.Validators
{
    public class ULongValidator : AbstractValidator<ulong>
    {
        public ULongValidator()
        {
            this.RuleFor(x => x).Must((x) => x > 0);
        }
    }
}
