using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace JsonByExampleGenerator.Generator.Models
{
    public class ClassModel
    {
        public string ClassName { get; set; }

        public List<PropertyModel> Properties { get; } = new List<PropertyModel>();

        public ClassModel(string className)
        {
            ClassName = className;
        }

        public void Merge(ClassModel classModel)
        {
            if (classModel != null)
            {
                Properties.AddRange(classModel.Properties.Except(this.Properties, new PropertyModelEqualityComparer()));
            }
        }
    }
}
