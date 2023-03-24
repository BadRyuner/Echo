using AsmResolver.DotNet.Signatures.Types;
using Echo.Platforms.AsmResolver.Emulation.Stack;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.ObjectModel
{
    /// <summary>
    /// Provides a base for instructions implementing unboxing behavior.
    /// </summary>
    public abstract class UnboxHandlerBase : CastOpCodeHandlerBase
    {
        /// <inheritdoc />
        protected override CilDispatchResult HandleSuccessfulCast(CilExecutionContext context, long objectAddress, TypeSignature targetType)
        {
            long dataAddress = objectAddress + context.Machine.ValueFactory.ObjectHeaderSize;
            context.CurrentFrame.EvaluationStack.Push(GetReturnValue(context, dataAddress, targetType));
            return CilDispatchResult.Success();
        }

        /// <summary>
        /// Transforms the resolved data address into a value to be pushed onto the stack.
        /// </summary>
        /// <param name="context">The context in which the instruction is emulated in.</param>
        /// <param name="dataAddress">The address.</param>
        /// <param name="targetType">The data type of the value at the address.</param>
        /// <returns>The return value.</returns>
        protected abstract StackSlot GetReturnValue(CilExecutionContext context, long dataAddress, TypeSignature targetType);
    }

}