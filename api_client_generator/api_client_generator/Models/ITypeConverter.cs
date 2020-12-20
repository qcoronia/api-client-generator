using System;

namespace api_client_generator.Models
{
    public interface ITypeConverter
    {
        public string Convert(Type type);
    }
}
