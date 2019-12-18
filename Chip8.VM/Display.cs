using System;
using System.Diagnostics;

namespace Chip8.VM
{
    public class Display
    {
        public const int ScreenWidth = 64;
        public const int ScreenHeight = 32;
        
        private readonly bool[,] _frameBuffer = new bool[ScreenWidth, ScreenHeight];
        
        public void ClearDisplay()
        {
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
        }

        public bool DrawSprite(byte x, byte y, Span<byte> spriteData)
        {
            Trace.WriteLine($"Drawing sprite at {x}, {y}. Data {string.Join(',', spriteData.ToArray())}");
            var collision = false;
            for (var row = 0; row < spriteData.Length; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    var pixelX = (x + col) % ScreenWidth;
                    var pixelY = (y + row) % ScreenHeight;
                    var currValue = _frameBuffer[pixelX, pixelY];
                    var newValue = ((spriteData[row] >> (7 - col)) & 0x01) == 1;

                    if (currValue && newValue)
                    {
                        collision = true;
                    }

                    _frameBuffer[pixelX, pixelY] = newValue ^ currValue;
                }
            }

            return collision;
        }

        public bool[,] GetCurrentFrame()
        {
            return _frameBuffer;
        }
    }
}