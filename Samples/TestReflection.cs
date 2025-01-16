using System;
using System.Reflection;
using System.Collections.Generic;

namespace Test_Reflection
{
    public class ModConfig
    {
        public bool Log = false;//undocumented, and unused right now.

        public bool MouseFix = true;

        public bool AutoSwing = false;
        public bool AutoSwingDagger = true;

        public bool SlickMoves = true;
        public bool SwordSpecialSlickMove = true;
        public bool ClubSpecialSlickMove { get; set; } = false;

        //undocumented internal items
        public float SlideVelocity = 2.8f;
        public float SpecialSlideVelocity = 2.8f;
        //public int TestInt = 1;
    }

    internal static partial class Enumerable
    {
        private static void CheckNotNull<T>(T value, string name) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(name);
        }

        /// <summary>
        /// Returns the input typed as <see cref="IEnumerable{T}"/>.
        /// </summary>

        public static IEnumerable<TSource> AsEnumerable<TSource>(IEnumerable<TSource> source)
        {
            return source;
        }

        /// <summary>
        /// Returns an empty <see cref="IEnumerable{T}"/> that has the 
        /// specified type argument.
        /// </summary>

        public static IEnumerable<TResult> Empty<TResult>()
        {
            return null;// Sequence<TResult>.Empty;
        }
        /// <summary>
        /// Converts the elements of an <see cref="IEnumerable"/> to the 
        /// specified type.
        /// </summary>

        public static IEnumerable<TResult> Cast<TResult>(
          this IEnumerable<TResult> source)
        {
            CheckNotNull(source, "source");

            var servesItself = source as IEnumerable<TResult>;
            if (servesItself != null
                && (!(servesItself is TResult[]) || servesItself.GetType().GetElementType() == typeof(TResult)))
            {
                return servesItself;
            }

            return CastYield<TResult>(source);
        }

        private static IEnumerable<TResult> CastYield<TResult>(
          IEnumerable<TResult> source)
        {
            foreach (var item in source)
                yield return (TResult)item;
        }
    }

    class Program
    {
        /// <summary>
        /// Determines whether the specified MemberInfo can be read.
        /// </summary>
        /// <param name="member">The MemberInfo to determine whether can be read.</param>
        /// /// <param name="nonPublic">if set to <c>true</c> then allow the member to be gotten non-publicly.</param>
        /// <returns>
        /// 	<c>true</c> if the specified MemberInfo can be read; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanReadMemberValue(MemberInfo member, bool nonPublic)
        {
            switch (member.MemberType)
            {
            case MemberTypes.Field:
                FieldInfo fieldInfo = (FieldInfo)member;

                if (nonPublic)
                {
                    return true;
                }
                else if (fieldInfo.IsPublic)
                {
                    return true;
                }
                return false;
            case MemberTypes.Property:
                PropertyInfo propertyInfo = (PropertyInfo)member;

                if (!propertyInfo.CanRead)
                {
                    return false;
                }
                if (nonPublic)
                {
                    return true;
                }
                return (propertyInfo.GetGetMethod(nonPublic) != null);
            default:
                return false;
            }
        }

        /// <summary>
        /// Determines whether the specified MemberInfo can be set.
        /// </summary>
        /// <param name="member">The MemberInfo to determine whether can be set.</param>
        /// <param name="nonPublic">if set to <c>true</c> then allow the member to be set non-publicly.</param>
        /// <param name="canSetReadOnly">if set to <c>true</c> then allow the member to be set if read-only.</param>
        /// <returns>
        /// 	<c>true</c> if the specified MemberInfo can be set; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSetMemberValue(MemberInfo member, bool nonPublic, bool canSetReadOnly)
        {
            switch (member.MemberType)
            {
            case MemberTypes.Field:
                FieldInfo fieldInfo = (FieldInfo)member;

                if (fieldInfo.IsLiteral)
                {
                    return false;
                }
                if (fieldInfo.IsInitOnly && !canSetReadOnly)
                {
                    return false;
                }
                if (nonPublic)
                {
                    return true;
                }
                if (fieldInfo.IsPublic)
                {
                    return true;
                }
                return false;
            case MemberTypes.Property:
                PropertyInfo propertyInfo = (PropertyInfo)member;

                if (!propertyInfo.CanWrite)
                {
                    return false;
                }
                if (nonPublic)
                {
                    return true;
                }
                return (propertyInfo.GetSetMethod(nonPublic) != null);
            default:
                return false;
            }
        }

        public static BindingFlags RemoveFlag(this BindingFlags bindingAttr, BindingFlags flag)
        {
            return ((bindingAttr & flag) == flag)
                ? bindingAttr ^ flag
                : bindingAttr;
        }

        public static IEnumerable<FieldInfo> GetFields(Type targetType, BindingFlags bindingAttr)
        {
            //ValidationUtils.ArgumentNotNull(targetType, nameof(targetType));

            List<FieldInfo> fieldInfos = new List<FieldInfo>(targetType.GetFields(bindingAttr));
            return fieldInfos;

            /*List<MemberInfo> fieldInfos = new List<MemberInfo>(targetType.GetFields(bindingAttr));
#if !PORTABLE
            // Type.GetFields doesn't return inherited private fields
            // manually find private fields from base class
            GetChildPrivateFields(fieldInfos, targetType, bindingAttr);
#endif
            return fieldInfos.Cast<FieldInfo>();*/
        }

/*#if !PORTABLE
        private static void GetChildPrivateFields(IList<MemberInfo> initialFields, Type targetType, BindingFlags bindingAttr)
        {
            // fix weirdness with private FieldInfos only being returned for the current Type
            // find base type fields and add them to result
            if ((bindingAttr & BindingFlags.NonPublic) != 0)
            {
                // modify flags to not search for public fields
                BindingFlags nonPublicBindingAttr = bindingAttr.RemoveFlag(BindingFlags.Public);

                while ((targetType = targetType.BaseType()) != null)
                {
                    // filter out protected fields
                    IEnumerable<FieldInfo> childPrivateFields =
                        targetType.GetFields(nonPublicBindingAttr).Where(f => f.IsPrivate);

                    initialFields.AddRange(childPrivateFields);
                }
            }
        }
#endif*/

        public static IEnumerable<PropertyInfo> GetProperties(Type targetType, BindingFlags bindingAttr)
        {
            //ValidationUtils.ArgumentNotNull(targetType, nameof(targetType));

            List<PropertyInfo> propertyInfos = new List<PropertyInfo>(targetType.GetProperties(bindingAttr));

            // GetProperties on an interface doesn't return properties from its interfaces
            if (targetType.IsInterface)
            {
                foreach (Type i in targetType.GetInterfaces())
                {
                    propertyInfos.AddRange(i.GetProperties(bindingAttr));
                }
            }

            //GetChildPrivateProperties(propertyInfos, targetType, bindingAttr);

            // a base class private getter/setter will be inaccessible unless the property was gotten from the base class
/*            for (int i = 0; i < propertyInfos.Count; i++)
            {
                PropertyInfo member = propertyInfos[i];
                if (member.DeclaringType != targetType)
                {
                    PropertyInfo declaredMember = (PropertyInfo)GetMemberInfoFromType(member.DeclaringType, member);
                    propertyInfos[i] = declaredMember;
                }
            }
*/
            return propertyInfos;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            ModConfig cfg = new ModConfig();

            //MemberInfo[] myMembers = cfg.GetType().GetMembers();
            //List<MemberInfo> myMembers = new List<MemberInfo>(cfg.GetType().GetMembers());

            //List<MemberInfo> myMembers = new(cfg.GetType().GetMembers());
            //List<MemberInfo> myMembers = new(cfg.GetType().GetFields());
            //List<MemberInfo> myMembers = new(cfg.GetType().GetProperties());

            BindingFlags binding = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            List<MemberInfo> myMembers = new List<MemberInfo>();
            myMembers.AddRange(GetFields(cfg.GetType(), binding));
            myMembers.AddRange(GetProperties(cfg.GetType(), binding));

            foreach (var item in myMembers)
            {
                switch (item.MemberType)
                {
                case MemberTypes.Field:
                    FieldInfo field = (FieldInfo)item;
                    Console.WriteLine($"field name={field.Name}, value={field.GetValue(cfg)}");
                    if (field.FieldType == typeof(bool))
                        Console.WriteLine("\tIs Boolean");
                    break;
                case MemberTypes.Property:
                    PropertyInfo property = (PropertyInfo)item;
                    Console.WriteLine($"property name={property.Name}, value={property.GetValue(cfg)}");
                    break;
                default:
                    Console.WriteLine($"Other member type = {item}");
                    break;
                }
            }
        }
    }
}
