namespace VltEd
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    [TypeConverter(typeof(CustomObjectConverter))]
    public class CustomObjectType
    {
        //[Category("Standard")]
        [Browsable(false)]
        public string Name { get; set; }

        [Browsable(false)]
        public List<CustomProperty> Properties { get; } = new List<CustomProperty>();

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public object this[string name]
        {
            get
            {
                _values.TryGetValue(name, out var val);
                return val;
            }

            set => _values[name] = value;
        }

        private class CustomObjectConverter : ExpandableObjectConverter
        {
            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            {
                var stdProps = base.GetProperties(context, value, attributes);
                var obj = value as CustomObjectType;
                var customProps = obj?.Properties;
                var props = new PropertyDescriptor[stdProps.Count + (customProps?.Count ?? 0)];
                stdProps.CopyTo(props, 0);
                if (customProps == null) return new PropertyDescriptorCollection(props);
                var index = stdProps.Count;
                foreach (var prop in customProps)
                {
                    props[index++] = new CustomPropertyDescriptor(prop);
                }
                return new PropertyDescriptorCollection(props);
            }
        }
        private class CustomPropertyDescriptor : PropertyDescriptor
        {
            private readonly CustomProperty _prop;
            public CustomPropertyDescriptor(CustomProperty prop) : base(prop.Name, null)
            {
                _prop = prop;
            }
            public override string Category => _prop.Category ?? "Dynamic";
            public override string Description => _prop.Desc;
            public override string Name => _prop.Name;

            public override bool ShouldSerializeValue(object component)
            {
                return ((CustomObjectType)component)[_prop.Name] != null;
            }

            public override void ResetValue(object component)
            {
                ((CustomObjectType)component)[_prop.Name] = null;
            }

            public override bool IsReadOnly => false;
            public override Type PropertyType => _prop.Type;

            public override bool CanResetValue(object component)
            {
                return true;
            }

            public override Type ComponentType => typeof(CustomObjectType);

            public override void SetValue(object component, object value)
            {
                ((CustomObjectType)component)[_prop.Name] = value;
            }

            public override object GetValue(object component)
            {
                return ((CustomObjectType)component)[_prop.Name] ?? _prop.DefaultValue;
            }
        }
    }


    public class CustomProperty
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Category { get; set; } = "Dynamic";
        public object DefaultValue { get; set; }
        private Type _type;

        public Type Type
        {
            get => _type;
            set
            {
                _type = value;
                DefaultValue = Activator.CreateInstance(value);
            }
        }
    }

}
