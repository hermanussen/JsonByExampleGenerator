using System;
using System.Linq;
using System.Collections.Generic;

namespace JsonByExampleGenerator.Generator.Models
{
    /// <summary>
    /// Represents a class that can be generated using Scriban.
    /// </summary>
    public class ClassModel
    {
        private static readonly string[] numericPropertyTypeOrder = new[]
        {
            "int",
            "long",
            "double",
            "decimal"
        };

        /// <summary>
        /// The name of the class, that should be valid in C#.
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// The properties that can be generated inside the class.
        /// </summary>
        public List<PropertyModel> Properties { get; } = new List<PropertyModel>();

        /// <summary>
        /// Create a new instance of this class based on its name.
        /// </summary>
        /// <param name="className"></param>
        public ClassModel(string className)
        {
            ClassName = className;
        }

        /// <summary>
        /// Takes properties from a similar ClassModel and adds them here, to get a more complete version of the ClassModel.
        /// </summary>
        /// <param name="classModel">The ClassModel to get additional properties from</param>
        public void Merge(ClassModel classModel)
        {
            if (classModel != null)
            {
                foreach(var property in classModel.Properties)
                {
                    var existingProp = Properties.FirstOrDefault(p => p.PropertyName == property.PropertyName);
                    if(existingProp == null)
                    {
                        Properties.Add(property);
                    }
                    else if(existingProp.PropertyType != property.PropertyType)
                    {
                        // If there is a less restrictive property type that is needed, it must be changed
                        if (numericPropertyTypeOrder.Contains(existingProp.PropertyType)
                            && numericPropertyTypeOrder.Contains(property.PropertyType)
                            && Array.IndexOf(numericPropertyTypeOrder, existingProp.PropertyType) < Array.IndexOf(numericPropertyTypeOrder, property.PropertyType))
                        {
                            existingProp.PropertyType = property.PropertyType;
                        }
                        else if (existingProp.PropertyType == "DateTime" && property.PropertyType == "string")
                        {
                            existingProp.PropertyType = property.PropertyType;
                        }
                    }
                }
            }
        }
    }
}
