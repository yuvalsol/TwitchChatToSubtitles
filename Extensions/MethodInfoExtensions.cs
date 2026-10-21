#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Reflection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

// https://gist.github.com/jamietre/4476307

public static partial class MethodInfoExtensions
{
    /// <summary>
    /// Returns the method signature as a string.
    /// </summary>
    ///
    /// <param name="method">
    /// The method.
    /// </param>
    /// <param name="callable">
    /// Return as an callable string ('public void a(string b)' would return 'a(b)').
    /// </param>
    ///
    /// <returns>
    /// Method signature.
    /// </returns>
    public static StringBuilder GetSignature(this MethodInfo method, bool callable = false)
    {
        var sigBuilder = new StringBuilder();

        BuildReturnSignature(sigBuilder, method, callable);

        BuildSignature(
            sigBuilder,
            parameters: method.GetParameters(),
            isExtensionMethod: method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false),
            method.IsGenericMethod,
            genericArguments: (method.IsGenericMethod ? method.GetGenericArguments() : null),
            callable
        );

        return sigBuilder;
    }

    /// <summary>
    /// Returns the constructor signature as a string.
    /// </summary>
    ///
    /// <param name="constructor">
    /// The constructor.
    /// </param>
    /// <param name="callable">
    /// Return as an callable string ('public a(string b)' would return 'a(b)').
    /// </param>
    ///
    /// <returns>
    /// Constructor signature.
    /// </returns>
    public static StringBuilder GetSignature(this ConstructorInfo constructor, bool callable = false)
    {
        var sigBuilder = new StringBuilder();

        BuildReturnSignature(sigBuilder, constructor, callable);

        BuildSignature(
            sigBuilder,
            parameters: constructor.GetParameters(),
            isExtensionMethod: false,
            constructor.IsGenericMethod,
            genericArguments: (constructor.IsGenericMethod ? constructor.GetGenericArguments() : null),
            callable
        );

        return sigBuilder;
    }

    private static void BuildSignature(
        StringBuilder sigBuilder,
        ParameterInfo[] parameters,
        bool isExtensionMethod,
        bool isGenericMethod,
        Type[] genericArguments,
        bool callable)
    {
        sigBuilder.Append('(');

        bool firstParam = true;
        bool secondParam = false;
        foreach (var param in parameters)
        {
            if (firstParam)
            {
                firstParam = false;

                if (isExtensionMethod)
                {
                    if (callable)
                    {
                        secondParam = true;
                        continue;
                    }

                    sigBuilder.Append("this ");
                }
            }
            else if (secondParam)
            {
                secondParam = false;
            }
            else
            {
                sigBuilder.Append(", ");
            }

            if (param.ParameterType.IsByRef)
            {
                if (param.IsIn)
                    sigBuilder.Append("in ");
                else if (param.IsOut)
                    sigBuilder.Append("out ");
                else
                    sigBuilder.Append("ref ");
            }

            bool isParamArray = param.GetCustomAttribute<ParamArrayAttribute>(true) != null;
            if (isParamArray)
                sigBuilder.Append("params ");

            if (callable == false)
            {
                BuildTypeSignature(sigBuilder, param);
                sigBuilder.Append(' ');
            }

            sigBuilder.Append(param.Name);

            if (param.IsOptional)
            {
                sigBuilder.Append(" = ");
                sigBuilder.Append(param.DefaultValue ?? "null");
            }
        }

        sigBuilder.Append(')');

        if (isGenericMethod == false)
            return;

        foreach (var arg in genericArguments)
        {
            List<string> constraints = [];
            foreach (var constraint in arg.GetGenericParameterConstraints())
            {
                var sb = new StringBuilder();
                BuildTypeSignature(sb, constraint);
                constraints.Add(sb.ToString());
            }

            var attrs = arg.GenericParameterAttributes;
            GenericParameterAttributes specialConstraints = attrs & GenericParameterAttributes.SpecialConstraintMask;

            if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint)
                constraints.Add("class");

            if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint)
                constraints.Add("struct");

            if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint)
                constraints.Add("new()");

            if (constraints.Count > 0)
            {
                sigBuilder.Append(" where ");
                BuildTypeSignature(sigBuilder, arg);
                sigBuilder.Append(": ");
                sigBuilder.Append(string.Join(", ", constraints));
            }
        }
    }

    /// <summary>
    /// Returns the property signature as a string.
    /// </summary>
    ///
    /// <param name="property">
    /// The property.
    /// </param>
    ///
    /// <returns>
    /// Property signature.
    /// </returns>
    public static StringBuilder GetSignature(this PropertyInfo property)
    {
        var sigBuilder = new StringBuilder();

        var getter = property.GetGetMethod();
        var setter = property.GetSetMethod();
        var primaryDef = LeastRestrictiveVisibility(getter, setter);

        BuildReturnSignature(sigBuilder, primaryDef, false);

        sigBuilder.Append(" { ");

        if (getter != null)
        {
            if (primaryDef != getter)
            {
                sigBuilder.Append(Visibility(getter));
                sigBuilder.Append(' ');
            }

            sigBuilder.Append("get; ");
        }

        if (setter != null)
        {
            if (primaryDef != setter)
            {
                sigBuilder.Append(Visibility(setter));
                sigBuilder.Append(' ');
            }

            sigBuilder.Append("set; ");
        }

        sigBuilder.Append('}');

        return sigBuilder;
    }

    private static void BuildTypeSignature(StringBuilder sigBuilder, ParameterInfo param)
    {
        BuildTypeSignature(
            sigBuilder,
            param.ParameterType,
            param.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>()?.TransformNames.ToList()
        );
    }

    private static void BuildTypeSignature(StringBuilder sigBuilder, Type type, List<string> valueTupleNames = null)
    {
        if (type.IsPointer)
        {
            BuildTypeSignature(sigBuilder, type.GetElementType(), valueTupleNames);
            sigBuilder.Append('*');
            return;
        }

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            BuildTypeSignature(sigBuilder, underlyingType, valueTupleNames);
            sigBuilder.Append('?');
            return;
        }

        string typeName = type.Name;
        if (typeName.EndsWith('&'))
            typeName = typeName.TrimEnd('&');

        if (type.IsGenericType)
        {
            typeName = typeName[..typeName.IndexOf('`')];

            if (typeName == "ValueTuple" && valueTupleNames?.Count > 0)
            {
                sigBuilder.Append('(');

                var genericArguments = type.GetGenericArguments();
                var names = valueTupleNames.GetRange(0, genericArguments.Length);
                valueTupleNames.RemoveRange(0, genericArguments.Length);
                int index = 0;
                bool first = true;
                foreach (var t in genericArguments)
                {
                    if (first == false)
                        sigBuilder.Append(", ");

                    string name = names[index++];
                    if (name != null)
                    {
                        BuildTypeSignature(sigBuilder, t, valueTupleNames);
                        sigBuilder.Append(' ');
                        sigBuilder.Append(name);
                    }
                    else
                    {
                        BuildTypeSignature(sigBuilder, t, valueTupleNames);
                    }

                    first = false;
                }

                sigBuilder.Append(')');
            }
            else
            {
                sigBuilder.Append(typeName);
                sigBuilder.Append('<');

                bool first = true;
                foreach (var t in type.GetGenericArguments())
                {
                    if (first == false)
                        sigBuilder.Append(',');
                    BuildTypeSignature(sigBuilder, t, valueTupleNames);
                    first = false;
                }

                sigBuilder.Append('>');
            }
        }
        else
        {
            if (type.IsArray)
            {
                BuildTypeSignature(sigBuilder, type.GetElementType(), valueTupleNames);
                sigBuilder.Append("[]");
                return;
            }

            sigBuilder.Append(typeName switch
            {
                "Boolean" or "System.Boolean" => "bool",
                "Byte" or "System.Byte" => "byte",
                "Char" or "System.Char" => "char",
                "Decimal" or "System.Decimal" => "decimal",
                "Double" or "System.Double" => "double",
                "Int32" or "System.Int32" => "int",
                "Int64" or "System.Int64" => "long",
                "Object" or "System.Object" => "object",
                "SByte" or "System.SByte" => "sbyte",
                "Int16" or "System.Int16" => "short",
                "String" or "System.String" => "string",
                "UInt32" or "System.UInt32" => "uint",
                "UInt64" or "System.UInt64" => "ulong",
                "UInt16" or "System.UInt16" => "ushort",
                "Void" => "void",
                _ => string.IsNullOrWhiteSpace(type.FullName) ? typeName : type.FullName.TrimEnd('&')
            });
        }
    }

    private static void BuildReturnSignature(StringBuilder sigBuilder, MethodInfo method, bool callable)
    {
        BuildReturnSignature(
            sigBuilder,
            visibility: (callable ? null : Visibility(method)),
            method.IsStatic,
            isSealed: method.IsVirtual && method.IsFinal,
            isAbstract: method.IsAbstract,
            isVirtual: method.IsVirtual && method.IsAbstract == false && method.GetBaseDefinition().DeclaringType == method.DeclaringType,
            isOverride: method.IsVirtual && method.IsAbstract == false && method.GetBaseDefinition().DeclaringType != method.DeclaringType,
            isUnsafe: IsUnsafe(method),
            returnParameter: (callable ? null : method.ReturnParameter),
            method.Name,
            method.IsGenericMethod,
            genericArguments: (method.IsGenericMethod ? method.GetGenericArguments() : null),
            callable
        );
    }

    private static void BuildReturnSignature(StringBuilder sigBuilder, ConstructorInfo constructor, bool callable)
    {
        BuildReturnSignature(
            sigBuilder,
            visibility: (callable ? null : Visibility(constructor)),
            constructor.IsStatic,
            isSealed: false,
            isAbstract: false,
            isVirtual: false,
            isOverride: false,
            isUnsafe: IsUnsafe(constructor),
            returnParameter: null,
            name: constructor.DeclaringType.ToString(),
            constructor.IsGenericMethod,
            genericArguments: (constructor.IsGenericMethod ? constructor.GetGenericArguments() : null),
            callable
        );
    }

    private static void BuildReturnSignature(
        StringBuilder sigBuilder,
        string visibility,
        bool isStatic,
        bool isSealed,
        bool isAbstract,
        bool isVirtual,
        bool isOverride,
        bool isUnsafe,
        ParameterInfo returnParameter,
        string name,
        bool isGenericMethod,
        Type[] genericArguments,
        bool callable)
    {
        if (callable == false)
        {
            sigBuilder.Append(visibility);
            sigBuilder.Append(' ');

            if (isStatic)
                sigBuilder.Append("static ");

            if (isSealed)
                sigBuilder.Append("sealed ");

            if (isAbstract)
                sigBuilder.Append("abstract ");

            if (isVirtual)
                sigBuilder.Append("virtual ");

            if (isOverride)
                sigBuilder.Append("override ");

            if (isUnsafe)
                sigBuilder.Append("unsafe ");

            if (returnParameter != null)
            {
                BuildTypeSignature(sigBuilder, returnParameter);
                sigBuilder.Append(' ');
            }
        }

        sigBuilder.Append(name);

        if (isGenericMethod == false)
            return;

        sigBuilder.Append('<');

        bool firstParam = true;
        foreach (var g in genericArguments)
        {
            if (firstParam)
                firstParam = false;
            else
                sigBuilder.Append(", ");

            BuildTypeSignature(sigBuilder, g);
        }

        sigBuilder.Append('>');
    }

    private static MethodInfo LeastRestrictiveVisibility(MethodInfo method1, MethodInfo method2)
    {
        if (method1 != null && method2 == null)
            return method1;

        if (method1 == null && method2 != null)
            return method2;

        return (VisibilityValue(method1) < VisibilityValue(method2) ? method1 : method2);
    }

    private static int VisibilityValue(MethodInfo method)
    {
        if (method.IsPublic) // public
            return 1;
        if (method.IsAssembly) // internal
            return 2;
        if (method.IsFamily) // protected
            return 3;
        if (method.IsFamilyOrAssembly) // protected internal
            return 4;
        if (method.IsFamilyAndAssembly) // private protected
            return 5;
        if (method.IsPrivate) // private
            return 6;
        return -1;
    }

    private static string Visibility(MethodBase method)
    {
        if (method.IsPublic)
            return "public";
        if (method.IsAssembly)
            return "internal";
        if (method.IsFamily)
            return "protected";
        if (method.IsFamilyOrAssembly)
            return "protected internal";
        if (method.IsFamilyAndAssembly)
            return "private protected";
        if (method.IsPrivate)
            return "private";
        return string.Empty;
    }

    public static bool IsUnsafe(MethodInfo method)
    {
        return
            // return type is a pointer
            method.ReturnType.IsPointer ||
            IsUnsafe((MethodBase)method);
    }

    public static bool IsUnsafe(MethodBase method)
    {
        return
            // any parameter types are a pointer
            method.GetParameters().Any(p => p.ParameterType.IsPointer) ||
            // any local variables are pinned in memory
            (method.GetMethodBody()?.LocalVariables.Any(local => local.IsPinned) ?? false);
    }
}
