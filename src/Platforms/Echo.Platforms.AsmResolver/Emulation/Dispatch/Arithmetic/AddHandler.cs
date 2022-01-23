using AsmResolver.PE.DotNet.Cil;
using Echo.Platforms.AsmResolver.Emulation.Stack;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.Arithmetic
{
    [DispatcherTableEntry(CilCode.Add, CilCode.Add_Ovf, CilCode.Add_Ovf_Un)]
    public class AddHandler : BinaryOpCodeHandlerBase
    {
        protected override bool IsSignedOperation(CilInstruction instruction)
        {
            return instruction.OpCode.Code is CilCode.Add or CilCode.Add_Ovf;
        }

        protected override CilDispatchResult Evaluate(CilExecutionContext context, CilInstruction instruction, 
            StackSlot argument1, StackSlot argument2)
        {
            var argument1Value = argument1.Contents.AsSpan();
            var argument2Value = argument2.Contents.AsSpan();
            
            if (argument1.TypeHint == StackSlotTypeHint.Integer)
                argument1Value.IntegerAdd(argument2Value);
            else
                argument1Value.FloatAdd(argument2Value);
            
            // TODO: overflow check.
            
            return CilDispatchResult.Success();
        }
    }
}