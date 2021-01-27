namespace JsonByExampleGenerator.Generator.Models
{
    /// <summary>
    /// Represents a property in a class that can be generated using Scriban.
    /// </summary>
    public class PropertyModel
    {
        /// <summary>
        /// The C# type of the property.
        /// </summary>
        public string PropertyType { get; internal set; }

        /// <summary>
        /// The C# safe to use name of the property.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// The original property name, before making it safe to use in C#.
        /// Can be used for example, for mapping back to json or comments.
        /// </summary>
        public string PropertyNameOriginal { get; private set; }

        /// <summary>
        /// If the property needs to have a default value, it can be specified here.
        /// </summary>
        public string? Init { get; internal set; }

        /// <summary>
        /// The order for output in json.
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// Create a new instance of the class.
        /// </summary>
        /// <param name="propertyNameOriginal">The original (unsafe) property name</param>
        /// <param name="propertyType">The C# type of the property</param>
        /// <param name="propertyName">The C# safe name of the property</param>
        /// <param name="order">The order for output</param>
        public PropertyModel(string propertyNameOriginal, string propertyType, string propertyName, int order)
        {
            PropertyNameOriginal = propertyNameOriginal;
            PropertyType = propertyType;
            PropertyName = propertyName;
            Order = order;
        }
    }
}