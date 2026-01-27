using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Services
{
 

    public class DialogService
    {
        // Event or callback to display dialog UI - this depends on your UI framework
        public Func<string, object, Task<object>> ShowDialogAsync { get; set; }

        public DialogService(Func<string, object, Task<object>> showDialogAsync)
        {
            ShowDialogAsync = showDialogAsync;
        }

        public async Task<object> ShowAsync<T>(string title, object parameters)
        {
            if (ShowDialogAsync == null)
                throw new InvalidOperationException("Dialog display function is not set.");

            // You can add additional logic here, e.g., handling parameters, dialog result, etc.
            var result = await ShowDialogAsync(title, parameters);
            return result;
        }
        // Confirm dialog method
        public async Task<bool> ConfirmDialog(string message, string title = "Confirmation")
        {
            if (ShowDialogAsync == null)
                throw new InvalidOperationException("Dialog display function is not set.");

            var parameters = new
            {
                Message = message,
                Title = title
            };

            var result = await ShowDialogAsync(title, parameters);

            // Assuming the dialog returns a bool indicating confirmation
            if (result is bool boolResult)
            {
                return boolResult;
            }

            // Default to false if result is not boolean
            return false;
        }

    }
}
