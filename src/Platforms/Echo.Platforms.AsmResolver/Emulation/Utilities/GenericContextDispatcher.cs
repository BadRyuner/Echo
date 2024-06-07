using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Echo.Platforms.AsmResolver.Emulation.Utilities;

/// <summary>
/// Maps data from GenericContext to signatures with !0
/// </summary>
public static class GenericContextDispatcher
{
    /// <summary>
    /// Safely resolves the field type, allowing for a possible Generic Context.
    /// </summary>
    /// <param name="field">Field, which we will resolve</param>
    /// <param name="genericContext">Current Generic Context</param>
    /// <returns></returns>
    public static TypeSignature? ResolveFieldType(this IFieldDescriptor field, GenericContext genericContext)
    {
        if (field == null)
        {
            return null;
        }

        if (genericContext.IsEmpty)
            genericContext = GenericContext.FromType(field.DeclaringType!);

        var fieldSignature = field.Signature!;

        if (fieldSignature.FieldType is GenericParameterSignature genericParameterSignature)
        {
            return genericContext.GetTypeArgument(genericParameterSignature);
        }

        return fieldSignature.FieldType;
    }

    /// <summary>
    /// Creates a MemberReference where !0's will be replaced with values from the GenericContext
    /// </summary>
    /// <param name="field">IFieldDescriptor, which we will resolve</param>
    /// <param name="genericContext">Current Generic Context</param>
    /// <returns></returns>
    public static MemberReference ResolveGenericField(this IFieldDescriptor field, in GenericContext genericContext)
    {
        var fieldSignature = new FieldSignature(field.ResolveFieldType(genericContext)!);
        var declaringType = field.DeclaringType!;
        var filledDeclaringType = genericContext.Type != null
            ? declaringType.ResolveGenericType(genericContext)
            : declaringType;
        var filledField = new MemberReference(filledDeclaringType.ToTypeDefOrRef(), field.Name, fieldSignature);

        return filledField;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="genericContext"></param>
    /// <returns></returns>
    public static ITypeDescriptor ResolveGenericType(this ITypeDescriptor type, in GenericContext genericContext)
    {
        var genericType = type.ToTypeSignature() as GenericInstanceTypeSignature;

        if (genericType == null)
            return type;

        var arguments = new TypeSignature[genericType.TypeArguments.Count];
        for (int i = 0; i < arguments.Length; i++)
        {
            var typeArgument = genericType.TypeArguments[i];
            if (typeArgument is GenericParameterSignature signature)
                arguments[i] = genericContext.GetTypeArgument(signature);
            else
                arguments[i] = typeArgument;
        }

        return genericType.GetUnderlyingTypeDefOrRef()!.MakeGenericInstanceType(arguments);
    }
}
