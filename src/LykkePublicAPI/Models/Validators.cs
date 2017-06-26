using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace LykkePublicAPI.Models
{
    /// <summary>
    /// Validates that the date is greater then date in specified field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private string _dateToCompareFieldName;

        public DateGreaterThanAttribute(string dateToCompareFieldName)
        {
            _dateToCompareFieldName = dateToCompareFieldName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var compareToValue = validationContext.ObjectType.GetProperty(_dateToCompareFieldName).GetValue(validationContext.ObjectInstance, null);
            if (value == null || compareToValue == null)
            {
                return new ValidationResult(string.Format("Dates {0}, {1} are not specified", validationContext.DisplayName, _dateToCompareFieldName));
            }

            DateTime laterDate = (DateTime)value;
            DateTime earlierDate = (DateTime)compareToValue;

            if (laterDate > earlierDate)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(string.Format("{0} should be greater than {1}", validationContext.DisplayName, _dateToCompareFieldName));
            }
        }
    }

    internal static class ModelExtensions
    {
        /// <summary>
        /// Converts ModelState errors to API error.
        /// </summary>
        public static ApiError ToApiError(this ModelStateDictionary modelState)
        {
            // Get first error message
            var error = modelState.Values.Where(e => e.ValidationState == ModelValidationState.Invalid)
                    .FirstOrDefault()?.Errors.FirstOrDefault();
            
            return new ApiError()
            {
                Code = ErrorCodes.InvalidInput,
                Msg = GetErrorMessage(error)
            };
        }

        /// <summary>
        /// Get any not empty error message
        /// </summary>
        private static string GetErrorMessage(ModelError error)
        {
            if (error == null)
            {
                return string.Empty;
            }
            else if (!string.IsNullOrEmpty(error.ErrorMessage))
            {
                return error.ErrorMessage;
            }
            else
            {
                // Get message of inner exception
                Exception ex = error.Exception;
                while(ex != null && ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                return ex?.Message ?? string.Empty;
            }
        }
    }
}
