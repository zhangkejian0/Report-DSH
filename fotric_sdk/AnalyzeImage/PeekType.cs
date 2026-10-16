using System;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

static class PeekType
{
    static int Main(string[] args)
    {
        Console.WriteLine(IRtekFileSDKValidator.Instance.IsValidFile(args[0]));
        return 0;
    }
}
