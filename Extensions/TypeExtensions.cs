using System;
using System.Linq.Expressions;

namespace Assets.RemoteHandsTracking.Extensions
{
    public static class TypeExtensions
    {
        public class FieldAccess<TType, TFieldType>
        {
            public TType Object { get; }
            private Action<TType, TFieldType> Setter { get; }
            private Func<TType, TFieldType> Getter { get; }

            public FieldAccess(TType @object, string fieldName)
            {
                Object = @object;
                Getter = typeof(TType).CreateGetFieldDelegate<TType, TFieldType>(fieldName);
                Setter = typeof(TType).CreateSetFieldDelegate<TType, TFieldType>(fieldName);
            }

            public TFieldType Get() => Getter(Object);
            public void Set(TFieldType val) => Setter(Object, val);
        }

        public static FieldAccess<TType, TFieldType> CreateFieldAccess<TType, TFieldType>(this Type type, TType obj, string fieldName, bool isGettingPropertyBackingField = false)
        {
            return new FieldAccess<TType, TFieldType>(obj, isGettingPropertyBackingField ? $"<{fieldName}>k__BackingField" : fieldName);
        }

        public static Func<TClass, TReturn> CreateGetFieldDelegate<TClass, TReturn>(this Type type, string fieldName)
        {
            var instExp = Expression.Parameter(type);
            var fieldExp = Expression.Field(instExp, fieldName);
            return Expression.Lambda<Func<TClass, TReturn>>(fieldExp, instExp).Compile();
        }

        public static Action<TClass, TSetValue> CreateSetFieldDelegate<TClass, TSetValue>(this Type type, string fieldName)
        {
            ParameterExpression targetExp = Expression.Parameter(typeof(TClass), "target");
            ParameterExpression valueExp = Expression.Parameter(typeof(TSetValue), "value");

            //cast the target from object to its correct type
            Expression castTartgetExp = type.IsValueType
                ? Expression.Unbox(targetExp, type)
                : Expression.Convert(targetExp, type);

            Expression castValueExp = Expression.Convert(valueExp, typeof(TSetValue));
            MemberExpression fieldExp = Expression.Field(castTartgetExp, fieldName);
            BinaryExpression assignExp = Expression.Assign(fieldExp, castValueExp);
            return Expression.Lambda<Action<TClass, TSetValue>>(assignExp, targetExp, valueExp).Compile();
        }
    }
}