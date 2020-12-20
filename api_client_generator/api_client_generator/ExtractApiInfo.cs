using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using api_client_generator.Models;
using Microsoft.AspNetCore.Mvc;

namespace api_client_generator
{
    public static class ExtractApiInfo
    {
        public static ApiClientInfo FromAssembly<TAssembly>(ApiExtractionOptions options)
            where TAssembly : class
        {
            if (options == null)
            {
                options = new ApiExtractionOptions
                {
                    TypeConverter = new DefaultTypeConverter(),
                };
            }

            var api = new ApiClientInfo
            {
                Areas = new List<AreaInfo>(),
                Models = new List<ModelInfo>(),
            };

            var controllers = typeof(TAssembly).Assembly
                .GetTypes()
                .Where(e => e.IsClass)
                .Where(e => e.Name.EndsWith("Controller"))
                .Where(e => e.GetCustomAttribute<AreaAttribute>() != null);

            var areas = controllers
                .GroupBy(e => e.GetCustomAttribute<AreaAttribute>().RouteValue);

            foreach (var areaControllers in areas)
            {
                var areaInfo = new AreaInfo
                {
                    Controllers = new List<ControllerInfo>(),
                };
                areaInfo.Name = areaControllers.Key.Substring(0, 1).ToLower() + areaControllers.Key.Substring(1);

                foreach (var controller in areaControllers)
                {
                    var controllerInfo = new ControllerInfo
                    {
                        Actions = new List<ActionInfo>(),
                    };

                    var area = controller.GetCustomAttribute<AreaAttribute>().RouteValue;
                    controllerInfo.Name = controller.Name.Substring(0, 1).ToLower() + controller.Name.Substring(1).Replace("Controller", string.Empty);
                    var getActions = controller.GetMethods()
                        .Where(e => e.GetCustomAttribute<HttpGetAttribute>() != null);
                    var postActions = controller.GetMethods()
                        .Where(e => e.GetCustomAttribute<HttpPostAttribute>() != null);
                    var putActions = controller.GetMethods()
                        .Where(e => e.GetCustomAttribute<HttpPutAttribute>() != null);

                    var actionOutputs = new List<string>();
                    foreach (var action in getActions)
                    {
                        var actionInfo = toActionInfo("get", controllerInfo, area, action);
                        controllerInfo.Actions.Add(actionInfo);
                    }

                    foreach (var action in postActions)
                    {
                        var actionInfo = toActionInfo("post", controllerInfo, area, action);
                        controllerInfo.Actions.Add(actionInfo);
                    }

                    foreach (var action in putActions)
                    {
                        var actionInfo = toActionInfo("put", controllerInfo, area, action);
                        controllerInfo.Actions.Add(actionInfo);
                    }

                    areaInfo.Controllers.Add(controllerInfo);
                }

                api.Areas.Add(areaInfo);
            }

            return api;

            ActionInfo toActionInfo(string verb, ControllerInfo controllerInfo, string area, MethodInfo action)
            {
                var actionInfo = new ActionInfo
                {
                    Verb = verb,
                    Params = new List<ParamInfo>(),
                };

                actionInfo.Name = action.Name.Substring(0, 1).ToLower() + action.Name.Substring(1);

                var routeTemplate = verb == "get" ? action.GetCustomAttribute<HttpGetAttribute>().Template
                    : verb == "post" ? action.GetCustomAttribute<HttpPostAttribute>().Template
                    : verb == "put" ? action.GetCustomAttribute<HttpPutAttribute>().Template
                    : string.Empty;

                var route = (routeTemplate ?? string.Empty).Replace("{", "${");
                actionInfo.Route = $"{area}/{controllerInfo.Name}/{route}".Trim('/');
                var headerParams = action.GetParameters()
                    .Where(e => e.GetCustomAttribute<FromHeaderAttribute>() != null);
                var routeParams = action.GetParameters()
                    .Where(e => e.GetCustomAttribute<FromRouteAttribute>() != null);
                var queryParams = action.GetParameters()
                    .Where(e => e.GetCustomAttribute<FromQueryAttribute>() != null);
                var bodyParams = action.GetParameters()
                    .Where(e => e.GetCustomAttribute<FromBodyAttribute>() != null);

                actionInfo.ReturnType = options.TypeConverter.Convert(action.ReturnType);
                ensureModelAdded(action.ReturnType);

                foreach (var routeParam in routeParams)
                {
                    actionInfo.Params.Add(new ParamInfo
                    {
                        Name = routeParam.Name,
                        Type = options.TypeConverter.Convert(routeParam.ParameterType),
                    });
                    ensureModelAdded(routeParam.ParameterType);
                }

                foreach (var queryParam in queryParams)
                {
                    actionInfo.Params.Add(new ParamInfo
                    {
                        Name = queryParam.Name,
                        Type = options.TypeConverter.Convert(queryParam.ParameterType),
                        IsOptional = true,
                    });
                    ensureModelAdded(queryParam.ParameterType);
                }

                foreach (var bodyParam in bodyParams)
                {
                    actionInfo.Params.Add(new ParamInfo
                    {
                        Name = bodyParam.Name,
                        Type = options.TypeConverter.Convert(bodyParam.ParameterType),
                        IsOptional = false,
                    });
                    ensureModelAdded(bodyParam.ParameterType);
                }

                actionInfo.HasPayload = (verb == "post" || verb == "put")
                    && bodyParams.Any();
                if (actionInfo.HasPayload)
                {
                    actionInfo.Payload = bodyParams.First().Name;
                }

                return actionInfo;
            }

            void ensureModelAdded(Type modelType)
            {
                var type = options.TypeConverter.Convert(modelType);

                if (api.Models.Any(m => m.Name == type.Replace("[]", string.Empty)))
                {
                    return;
                }

                if (type == "any"
                    || type == "number"
                    || type == "string"
                    || type == "boolean"
                    || type == "Date"
                    || type == "Guid")
                {
                    return;
                }

                var actualType = modelType;
                if (modelType.IsGenericType && modelType.Name.Contains(nameof(Task)))
                {
                    actualType = modelType.GetGenericArguments().First();
                }

                if (modelType.IsGenericType && modelType.Name.Contains(typeof(IEnumerable<>).Name))
                {
                    actualType = modelType.GetGenericArguments().First();
                }

                var model = new ModelInfo
                {
                    Name = type.Replace("[]", string.Empty),
                    Properties = actualType.GetProperties()
                        .Select(p =>
                        {
                            var pi = new PropInfo
                            {
                                Name = p.Name,
                                Type = options.TypeConverter.Convert(p.PropertyType),
                                IsOptional = p.PropertyType.Name.Contains(typeof(Nullable<>).Name)
                            };

                            ensureModelAdded(p.PropertyType);

                            return pi;
                        })
                        .ToList(),
                };

                api.Models.Add(model);
            }
        }
    }
}
