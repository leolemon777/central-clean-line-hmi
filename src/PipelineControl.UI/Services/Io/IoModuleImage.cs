namespace PipelineControl.UI.Services.Io;

public sealed record IoModuleImage(int ModuleIndex, int Value, int BitCount = 16)
{
    public bool GetBit(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= 32)
        {
            return false;
        }

        return (Value & (1 << bitIndex)) != 0;
    }
}
