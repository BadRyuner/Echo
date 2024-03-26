using AsmResolver.PE.DotNet.Cil;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.Pointers;

[DispatcherTableEntry(CilCode.Unaligned)]
public class UnalignedHandler : FallThroughOpCodeHandler
{
    /// <inheritdoc />
    protected override CilDispatchResult DispatchInternal(CilExecutionContext context, CilInstruction instruction)
    {
        // Current virtual memory model does not distinguish between aligned and unaligned read/writes, so this
        // opcode prefix is just a NOP for now.
        
        return CilDispatchResult.Success();
    }
}