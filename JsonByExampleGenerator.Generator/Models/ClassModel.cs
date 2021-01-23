using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace JsonByExampleGenerator.Generator.Models
{
    /// <summary>
    /// Represents a class that can be generated using Scriban.
    /// </summary>
    public class ClassModel
    {
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
                Properties.AddRange(classModel.Properties.Except(this.Properties, new PropertyModelEqualityComparer()));
            }
        }
    }
}
