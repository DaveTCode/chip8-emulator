using System;
using System.Collections.Generic;
using System.Diagnostics;
using Chip8.Console.SDL2;
using Chip8.VM;

namespace Chip8.Console
{
    public class Sdl2Application : IDisposable
    {
        private readonly Dictionary<SDL.SDL_Keycode, byte> _keyMap = new Dictionary<SDL.SDL_Keycode, byte>()
        {
            { SDL.SDL_Keycode.SDLK_0, 0x00},
            { SDL.SDL_Keycode.SDLK_1, 0x01},
            { SDL.SDL_Keycode.SDLK_2, 0x02},
            { SDL.SDL_Keycode.SDLK_3, 0x03},
            { SDL.SDL_Keycode.SDLK_4, 0x04},
            { SDL.SDL_Keycode.SDLK_5, 0x05},
            { SDL.SDL_Keycode.SDLK_6, 0x06},
            { SDL.SDL_Keycode.SDLK_7, 0x07},
            { SDL.SDL_Keycode.SDLK_8, 0x08},
            { SDL.SDL_Keycode.SDLK_9, 0x09},
            { SDL.SDL_Keycode.SDLK_a, 0x0a},
            { SDL.SDL_Keycode.SDLK_b, 0x0b},
            { SDL.SDL_Keycode.SDLK_c, 0x0c},
            { SDL.SDL_Keycode.SDLK_d, 0x0d},
            { SDL.SDL_Keycode.SDLK_e, 0x0e},
            { SDL.SDL_Keycode.SDLK_f, 0x0f}
        };

        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly int _pixelSize;
        private readonly int _screenWidth;
        private readonly int _screenHeight;

        private readonly Computer _computer;

        internal Sdl2Application(Computer computer, int pixelSize)
        {
            _computer = computer;
            _pixelSize = pixelSize;
            _screenWidth = Display.ScreenWidth * pixelSize;
            _screenHeight = Display.ScreenHeight * pixelSize;

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            SDL.SDL_CreateWindowAndRenderer(
                _screenWidth,
                _screenHeight,
                0,
                out _window,
                out _renderer);
            SDL.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(_renderer);
        }

        public void ExecuteProgram(int ticksPerSecond)
        {
            var stopwatch = Stopwatch.StartNew();
            var msPerCycle = (1.0 / ticksPerSecond) * 1000;

            var quit = false;
            while (!quit)
            {
                while (SDL.SDL_PollEvent(out var e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            quit = true;
                            break;
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            if (_keyMap.ContainsKey(e.key.keysym.sym))
                            {
                                _computer.KeyDown(_keyMap[e.key.keysym.sym]);
                            }
                            break;
                        case SDL.SDL_EventType.SDL_KEYUP:
                            if (_keyMap.ContainsKey(e.key.keysym.sym))
                            {
                                _computer.KeyUp(_keyMap[e.key.keysym.sym]);
                            }
                            break;
                    }
                }

                _computer.Tick();

                if (_computer.Display.NeedsRedraw)
                {
                    var frameBuffer = _computer.Display.GetCurrentFrame();

                    for (var x = 0; x < frameBuffer.GetLength(0); x++)
                    {
                        for (var y = 0; y < frameBuffer.GetLength(1); y++)
                        {
                            SDL.SDL_SetRenderDrawColor(_renderer,
                                (byte)(frameBuffer[x, y] ? 255 : 0),
                                (byte)(frameBuffer[x, y] ? 255 : 0),
                                (byte)(frameBuffer[x, y] ? 255 : 0),
                                255);

                            var rect = new SDL.SDL_Rect
                            {
                                x = x * _pixelSize,
                                y = y * _pixelSize,
                                h = _pixelSize,
                                w = _pixelSize,
                            };
                            SDL.SDL_RenderFillRect(_renderer, ref rect);
                        }
                    }

                    SDL.SDL_RenderPresent(_renderer);
                }

                var msToSleep = msPerCycle - (stopwatch.ElapsedTicks / Stopwatch.Frequency) * 1000;
                if (msToSleep > 0)
                {
                    SDL.SDL_Delay((uint) msToSleep);
                }
            }
        }

        public void Dispose()
        {
            SDL.SDL_DestroyRenderer(_renderer);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }
    }
}