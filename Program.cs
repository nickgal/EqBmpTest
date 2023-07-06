using EqBmpTest;

// https://github.com/mono/libgdiplus/issues/702
// https://github.com/Robmaister/SharpFont/issues/55
// https://github.com/Robmaister/SharpFont/pull/136

if (args.Length < 1)
{
    Console.WriteLine("Missing args");
    return;
}

var inputFilePath = args[0];

if (!File.Exists(inputFilePath))
{
    Console.WriteLine("Input file not found");
    return;
}

var inputFullPath = Path.GetFullPath(inputFilePath);
var outputFilePath = Path.ChangeExtension(inputFullPath, ".png");

using var fs = File.OpenRead(inputFilePath);
var bmp = new EqBmp(fs);

Console.WriteLine($"Saving transparent image {outputFilePath}");
bmp.WritePng(outputFilePath);
