using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace diplom_project.Controllers
{
    public class TimeSpanModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            if (TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out var timeSpan))
            {
                bindingContext.Result = ModelBindingResult.Success(timeSpan);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid time format. Use HH:mm (e.g., 13:30).");
            return Task.CompletedTask;
        }
    }
}
