using System.Drawing;
using System.Drawing.Imaging;

namespace EqBmpTest
{
    enum PaletteFlags
    {
        HasAlpha = 0x0001,
        GrayScale = 0x0002,
        HalfTone = 0x0004,
    }

    public class EqBmp
    {
        private static readonly bool _needsGdipHacks = Environment.OSVersion.Platform == PlatformID.MacOSX ||
            Environment.OSVersion.Platform == PlatformID.Unix;
        private static bool _hasCheckedForPaletteFlagsField;
        private static System.Reflection.FieldInfo _paletteFlagsField = null!;
        private readonly Bitmap _bitmap;
        private readonly ColorPalette _palette;

        public EqBmp(Stream stream)
        {
            SetPaletteFlagsField();

            _bitmap = new Bitmap(stream);
            _palette = _bitmap.Palette;
        }

        public void WritePng(string outputFilePath)
        {
            MakeTransparent();
            _bitmap.Save(outputFilePath, ImageFormat.Png);
        }

        private void MakeTransparent()
        {
            switch (_bitmap.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    var transparentIndex = 0;
                    MakePaletteTransparent(transparentIndex);
                    break;
                default:
                    MakeMagentaTransparent();
                    break;
            }
        }

        private void MakeMagentaTransparent()
        {
            _bitmap.MakeTransparent(Color.Magenta);
            if (_needsGdipHacks)
            {
                // https://github.com/mono/libgdiplus/commit/bf9a1440b7bfea704bf2cb771f5c2b5c09e7bcfa
                _bitmap.MakeTransparent(Color.FromArgb(0, Color.Magenta));
            }
        }

        private void MakePaletteTransparent(int transparentIndex)
        {
            if (_needsGdipHacks)
            {
                _paletteFlagsField?.SetValue(_palette, _palette.Flags | (int)PaletteFlags.HasAlpha);
            }

            var transparentColor = GetTransparentPaletteColor();
            _palette.Entries[transparentIndex] = transparentColor;
            _bitmap.Palette = _palette;

            if (_needsGdipHacks)
            {
                _bitmap.MakeTransparent(transparentColor);
            }
        }

        private Color GetTransparentPaletteColor()
        {
            var transparencyColor = Color.FromArgb(0, 0, 0, 0);

            if (!_needsGdipHacks)
            {
                return transparencyColor;
            }

            var random = new Random();
            var foundUnique = false;

            while (!foundUnique)
            {
                foundUnique = _palette.Entries.All(e => e != transparencyColor);
                transparencyColor = Color.FromArgb(0, random.Next(256), random.Next(256), random.Next(256));
            }

            return transparencyColor;
        }

        private static void SetPaletteFlagsField()
        {
            if (!_needsGdipHacks || _hasCheckedForPaletteFlagsField)
            {
                return;
            }

            _hasCheckedForPaletteFlagsField = true;

            // The field needed may be named "flags" or "_flags", dependin on the version of Mono. To be thorough, check for the first Name that contains "lags".
            var fields = typeof(ColorPalette).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // https://github.com/Robmaister/SharpFont/blob/422bdab059dd8e594b4b061a3b53152e71342ce2/Source/SharpFont.GDI/FTBitmapExtensions.cs
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name.Contains("lags"))
                {
                    _paletteFlagsField = fields[i];
                    break;
                }
            }
        }

    }
}
