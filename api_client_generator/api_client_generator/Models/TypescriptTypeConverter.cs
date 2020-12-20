using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace api_client_generator.Models
{
    public class TypescriptTypeConverter : ITypeConverter
    {
        public string Convert(Type type)
        {
            if (type.IsGenericType)
            {
                var typeParam = type.GetGenericArguments().First();
                if (type.Name.Contains(nameof(IEnumerable)))
                {
                    return $"{this.Convert(typeParam)}[]";
                }
                else if (type.Name.Contains(nameof(Task)))
                {
                    return this.Convert(typeParam);
                }
                else if (type.Name.Contains(nameof(Nullable)))
                {
                    return this.Convert(typeParam);
                }

                var types = string.Join(',', type.GetGenericArguments()
                    .Select(t => this.Convert(t)))
                    .Replace(",", ", ")
                    .Trim();

                return $"{type.Name}<{types}>";
            }

            var conversion = new Dictionary<string, string>
            {
                [typeof(int).Name] = "number",
                [typeof(bool).Name] = "boolean",
                [typeof(double).Name] = "number",
                [typeof(decimal).Name] = "number",
                [typeof(string).Name] = "string",
                [typeof(Guid).Name] = "string",
                [typeof(DateTime).Name] = "Date",
                [typeof(IActionResult).Name] = "any",
                [typeof(void).Name] = "any",
                [typeof(Task).Name] = "any",
            };
            if (!conversion.ContainsKey(type.Name))
            {
                conversion.Add(type.Name, type.Name);
            }

            return conversion[type.Name];
        }
    }
}
