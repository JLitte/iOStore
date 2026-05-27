using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace iOStore.Helpers
{
    /// <summary>
    /// Parsea campos decimal/decimal? usando InvariantCulture (punto como separador
    /// decimal). Necesario porque el JavaScript del checkout envía los valores con
    /// toFixed(2) — formato anglosajón — mientras que el model binder de ASP.NET Core
    /// en una máquina con cultura es-AR interpretaría el punto como separador de miles,
    /// multiplicando el valor por 100 (ej: "1500.00" → 150000).
    /// </summary>
    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(decimal) ||
                context.Metadata.ModelType == typeof(decimal?))
                return new InvariantDecimalModelBinder(context.Metadata.ModelType);

            return null;
        }
    }

    public class InvariantDecimalModelBinder : IModelBinder
    {
        private readonly Type _modelType;

        public InvariantDecimalModelBinder(Type modelType) => _modelType = modelType;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == ValueProviderResult.None) return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

            var raw = valueResult.FirstValue;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (_modelType == typeof(decimal?))
                    bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            // InvariantCulture primero: JS siempre envía "1500.00" (punto decimal)
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            // Fallback a cultura del servidor (formularios admin que puedan usar coma)
            else if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"El valor '{raw}' no es un número decimal válido.");
            }

            return Task.CompletedTask;
        }
    }
}
