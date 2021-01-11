using System;
using System.Collections.Generic;
using System.Text;

namespace JsonByExampleGenerator.Generator.Models
{
    public class PropertyModelEqualityComparer : IEqualityComparer<PropertyModel>
    {
        public bool Equals(PropertyModel x, PropertyModel y)
        {
            return string.Equals(x?.PropertyName, y?.PropertyName, StringComparison.InvariantCulture);
        }

        public int GetHashCode(PropertyModel obj)
        {
            return obj?.PropertyName.GetHashCode() ?? -1;
        }
    }
}
