using System;
using FluentValidation;

namespace KeyValueStore.Validators
{
    public class ValueValidator: AbstractValidator<string>
    {
        public ValueValidator()
        {
            RuleFor(x => x).MaximumLength(5120); // 5kb
            RuleFor(x => x).Must(x =>
            {
                try
                {
                   _ = Convert.FromBase64String(x);
                    return x.Replace(" ","").Length % 4 == 0;
                }
                catch
                {
                    return false;
                }
            }).WithMessage("The content must be a base64 encoded string with maximum length 5kb.");
        }
    }
}