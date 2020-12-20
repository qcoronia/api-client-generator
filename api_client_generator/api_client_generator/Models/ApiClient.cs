using System.Collections.Generic;

namespace api_client_generator.Models
{
    public class ApiClientInfo
    {
        public List<AreaInfo> Areas { get; set; }
        public List<ModelInfo> Models { get; set; }
    }

    public class AreaInfo
    {
        public string Name { get; set; }
        public List<ControllerInfo> Controllers { get; set; }
    }

    public class ControllerInfo
    {
        public string Name { get; set; }
        public List<ActionInfo> Actions { get; set; }
    }

    public class ActionInfo
    {
        public string Name { get; set; }
        public List<ParamInfo> Params { get; set; }
        public string Route { get; set; }
        public string Verb { get; set; }
        public string ReturnType { get; set; }
        public bool HasPayload { get; set; }
        public string Payload { get; set; }
    }

    public class ParamInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOptional { get; set; }
    }

    public class ModelInfo
    {
        public string Name { get; set; }
        public List<PropInfo> Properties { get; set; }
    }

    public class PropInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOptional { get; set; }
    }
}
