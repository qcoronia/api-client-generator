using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using api_client_generator.Models;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;

// reference the api with `Startup` class here
// using My.Api;

namespace api_client_generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            GenerateFromReflection(args);

            // GenerateByStringBuilder(args);
        }

        private static void GenerateFromReflection(string[] args)
        {
            var templateFile = File.ReadAllText("Templates/api-client.ts.template");
            var template = Handlebars.Compile(templateFile);

            // Make sure the project that contains `Startup` is referenced
            var api = ExtractApiInfo.FromAssembly<Startup>(new ApiExtractionOptions
            {
                TypeConverter = new TypescriptTypeConverter(),
            });

            var output = template(api);
            File.WriteAllText("../../../api-client.ts", output);
        }

        private static void GenerateByStringBuilder(string[] args)
        {
            var controllers = typeof(Startup).Assembly
                .GetTypes()
                .Where(e => e.IsClass)
                .Where(e => e.Name.EndsWith("Controller"))
                .Where(e => e.GetCustomAttribute<AreaAttribute>() != null);

            var areas = controllers
                .GroupBy(e => e.GetCustomAttribute<AreaAttribute>().RouteValue);

            var headerTemplate = File.ReadAllText("Templates/api-client-ts.header.template");
            var output = new StringBuilder();

            foreach (var areaControllers in areas)
            {
                var areaName = areaControllers.Key.Substring(0, 1).ToLower() + areaControllers.Key.Substring(1);
                output.AppendLine($"  public {areaName} = {{");

                foreach (var controller in areaControllers)
                {
                    var area = controller.GetCustomAttribute<AreaAttribute>().RouteValue;
                    var controllerName = controller.Name.Substring(0, 1).ToLower() + controller.Name.Substring(1).Replace("Controller", string.Empty);
                    var getActions = controller.GetMethods()
                        .Where(e => e.GetCustomAttribute<HttpGetAttribute>() != null);
                    var postActions = controller.GetMethods()
                        .Where(e => e.GetCustomAttribute<HttpPostAttribute>() != null);
                    var putActions = controller.GetMethods()
                        .Where(e => e.GetCustomAttribute<HttpPutAttribute>() != null);

                    output.AppendLine($"    {controllerName}: {{");

                    var actionOutputs = new List<string>();
                    foreach (var action in getActions)
                    {
                        var actionName = action.Name.Substring(0, 1).ToLower() + action.Name.Substring(1);
                        var route = (action.GetCustomAttribute<HttpGetAttribute>().Template ?? string.Empty).Replace("{", "${");
                        route = $"{area}/{controllerName}/{route}".Trim('/');
                        var headerParams = action.GetParameters()
                            .Where(e => e.GetCustomAttribute<FromHeaderAttribute>() != null);
                        var routeParams = action.GetParameters()
                            .Where(e => e.GetCustomAttribute<FromRouteAttribute>() != null);
                        var queryParams = action.GetParameters()
                            .Where(e => e.GetCustomAttribute<FromQueryAttribute>() != null);
                        var returnType = action.ReturnType.Name;
                        if (returnType.StartsWith(nameof(Task)))
                        {
                            if (action.ReturnType.GenericTypeArguments.Any())
                            {
                                returnType = action.ReturnType.GenericTypeArguments.First().Name;
                            }
                        }

                        if (returnType.StartsWith(nameof(IActionResult)))
                        {
                            returnType = "any";
                        }

                        var actionOutput = new StringBuilder();

                        actionOutput.Append($"  ");
                        actionOutput.Append($"  ");
                        actionOutput.Append($"  ");
                        actionOutput.Append($"{actionName}: (");

                        var paramOutput = string.Join(", ",
                            string.Join(", ", routeParams
                                .Select(param => (paramName: param.Name.Substring(0, 1).ToLower() + param.Name.Substring(1), paramType: param.ParameterType.Name))
                                .Select(e => $"{e.paramName}: {e.paramType}")),
                            string.Join(", ", queryParams
                                .Select(param => (paramName: param.Name.Substring(0, 1).ToLower() + param.Name.Substring(1), paramType: param.ParameterType.Name))
                                .Select(e => $"{e.paramName}?: {e.paramType}")));
                        paramOutput = paramOutput
                            .Replace("Int32", "number")
                            .Replace("decimal", "number")
                            .Replace("Guid", "string")
                            .Replace("DateTime", "Date")
                            .Trim(',').Trim();
                        actionOutput.Append(paramOutput);

                        if (queryParams.Any())
                        {
                            route += "?" + string.Join("&", queryParams
                                .Select(param => param.Name.Substring(0, 1).ToLower() + param.Name.Substring(1))
                                .Select(e => $"{e}=${{{e}}}"));
                        }

                        actionOutput.Append($") => this.get<{returnType}>(`{route}`)");
                        actionOutputs.Add(actionOutput.ToString());
                    }

                    output.AppendJoin(",\r\n", actionOutputs);

                    if (actionOutputs.Any())
                    {
                        output.AppendLine(string.Empty);
                    }

                    output.AppendLine("    },");
                }

                output.AppendLine("  };");
            }

            output.Append("}\r\n");

            var finalOutput = output.ToString();
            File.WriteAllText("output.ts", finalOutput);
        }
    }
}
