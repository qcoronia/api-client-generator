using System;

namespace api_client_generator.Models
{
    public class DefaultTypeConverter : ITypeConverter
    {
        public string Convert(Type type)
        {
            return type.Name;
        }
    }
}
