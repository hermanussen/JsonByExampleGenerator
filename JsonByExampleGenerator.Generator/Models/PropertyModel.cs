namespace JsonByExampleGenerator.Generator.Models
{
    public class PropertyModel
    {
        public string PropertyType { get; private set; }
        public string PropertyName { get; private set; }
        public object PropertyNameOriginal { get; private set; }

        public string? Init { get; set; }

        public PropertyModel(string propertyNameOriginal, string propertyType, string propertyName)
        {
            PropertyNameOriginal = propertyNameOriginal;
            PropertyType = propertyType;
            PropertyName = propertyName;
        }
    }
}