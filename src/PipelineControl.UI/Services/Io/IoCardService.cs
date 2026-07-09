using System.Diagnostics;

namespace PipelineControl.UI.Services.Io;

public interface IIoCardService
{
    Task SetOutputAsync(string tagAddress, bool value);
}

public sealed class MockIoCardService : IIoCardService
{
    private readonly IoBoardService? ioBoardService;

    public MockIoCardService()
    {
    }

    public MockIoCardService(IoBoardService ioBoardService)
    {
        this.ioBoardService = ioBoardService;
    }

    public async Task SetOutputAsync(string tagAddress, bool value)
    {
        if (ioBoardService is null)
        {
            Debug.WriteLine($"IO mock: {tagAddress} = {(value ? "开" : "关")}");
            return;
        }

        var result = await ioBoardService.WriteOutputByTagAsync(tagAddress, value).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Message);
        }
    }
}
