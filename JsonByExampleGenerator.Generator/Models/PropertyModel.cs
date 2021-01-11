namespace JsonByExampleGenerator.Generator.Models
{
    public class PropertyModel
    {
        public string PropertyType { get; private set; }
        public string PropertyName { get; private set; }

        public string? Init { get; set; }

        public PropertyModel(string propertyType, string propertyName)
        {
            PropertyType = propertyType;
            PropertyName = propertyName;
        }
    }
}