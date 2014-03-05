using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;

namespace OuiGui.WPF.Util
{
    // http://stackoverflow.com/a/4398752
    public class EnumerationExtension : MarkupExtension
    {
        private Type enumType;

        public EnumerationExtension(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");

            this.EnumType = enumType;
        }

        public Type EnumType
        {
            get { return this.enumType; }
            private set
            {
                if (this.enumType == value)
                    return;

                var underlyingType = Nullable.GetUnderlyingType(value) ?? value;

                if (!underlyingType.IsEnum)
                    throw new ArgumentException("Type must be an Enum.");

                this.enumType = value;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var enumValues = Enum.GetValues(EnumType);

            return (
              from object enumValue in enumValues
              select new EnumerationMember
              {
                  Value = enumValue,
                  Description = GetDescription(enumValue)
              }).ToArray();
        }

        private string GetDescription(object enumValue)
        {
            var descriptionAttribute = EnumType
              .GetField(enumValue.ToString())
              .GetCustomAttributes(typeof(DescriptionAttribute), false)
              .FirstOrDefault() as DescriptionAttribute;

            return descriptionAttribute != null
              ? descriptionAttribute.Description
              : enumValue.ToString();
        }

        public class EnumerationMember
        {
            public string Description { get; set; }
            public object Value { get; set; }
        }
    }
}
