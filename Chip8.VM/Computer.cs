using System;
using System.Diagnostics;
using Chip8.VM.Displays;
using Chip8.VM.Keyboards;

namespace Chip8.VM
{
    // Public API into the chip 8 VM. Exposes functions to load a program into
    // memory and then execute it.
    public class Computer
    {
        private const ushort FontMemoryLocation = 0x0;
        private const byte StackSize = 0xf;

        // 4096 byte RAM
        private readonly byte[] _memory = new byte[4096];

        // 16 8 bit Registers V0 - VF
        private readonly byte[] _registers = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

        // 16 bit memory register I
        private ushort _i;

        // 16 bit instruction pointer PC
        private ushort _programCounter;

        // 8 bit delay timer register
        private byte _delayTimerRegister;

        // 8 bit sound timer register
        private byte _soundTimerRegister;

        // 16 16 bit stack values 
        private byte _stackPointer;
        private readonly ushort[] _stack = new ushort[StackSize];

        // Display is injected to provide different UIs
        private readonly Display _display = new Display();

        // Keyboard is injected to provide different input methods
        private readonly IKeyboard _keyboard;

        // Used to generate random numbers
        private readonly Random _random;
        
        // The number of ticks before we decrement the timer/sound registers
        // (they decrement at 60hz)
        private readonly int _ticksPerTimerDecrement;

        // A count of the total ticks to show how long the computer has been
        // running and whether we should decrement the timer/sound registers
        private long _totalTicks;

        public Computer(IKeyboard keyboard, Random random, int ticksPerSecond)
        {
            _keyboard = keyboard;
            _random = random;
            _ticksPerTimerDecrement = ticksPerSecond / 60;
        }

        private void LoadFont()
        {
            Array.Copy(Font.FontData, 0, _memory, FontMemoryLocation, Font.FontData.Length);
        }

        /// <summary>
        /// Takes a program already read into a byte array and loads it into
        /// the VMs memory.
        /// </summary>
        /// 
        /// <param name="program">Non empty program.</param>
        /// <param name="programType">Specifies where this program will start in memory.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the program does not fit into the VMs memory</exception>
        public void LoadProgramAndReset(byte[] program, ProgramType programType)
        {
            if (program.Length + programType.MemoryStartAddress() > _memory.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(program), program, "Program doesn't fit into memory");
            }
            
            Array.Clear(_registers, 0, _registers.Length);
            Array.Clear(_memory, 0, _memory.Length);
            _i = 0;
            _programCounter = programType.MemoryStartAddress();
            _delayTimerRegister = 0;
            _soundTimerRegister = 0;
            _stackPointer = 0;
            Array.Clear(_stack, 0, _stack.Length);
            _display.ClearDisplay();
            _totalTicks = 0L;
            
            LoadFont();
            Array.Copy(program, 0, _memory, programType.MemoryStartAddress(), program.Length);
        }
        
        /// <summary>
        /// Get the full state of the current frame for the renderer to process.
        /// </summary>
        /// <returns>A span containing the frame with true for pixel ON and
        /// false for pixel OFF.</returns>
        public bool[,] GetCurrentFrame()
        {
            return _display.GetCurrentFrame();
        }

        /// <summary>
        /// Executes the next instruction and handles sound/timer registers
        /// </summary>
        public void Tick()
        {
            // All instructions are 2 bytes 0x
            var opcode = _memory[_programCounter] << 8 | _memory[_programCounter + 1];
            _programCounter += 2;
            Trace.WriteLine(StateToString(opcode));
            ProcessInstruction(opcode);
            _totalTicks++;

            if (_totalTicks % _ticksPerTimerDecrement != 0) return;
            
            if (_delayTimerRegister > 0)
            {
                Trace.WriteLine($"DT {_delayTimerRegister}--");
                _delayTimerRegister--;
            }

            if (_soundTimerRegister > 0)
            {
                Trace.WriteLine($"ST {_delayTimerRegister}--");
                _soundTimerRegister--;
            }
            
            Trace.Flush();
        }

        private void ProcessInstruction(int opcode)
        {
            var kk = (byte) (opcode & 0x00FF);
            var n1 = (opcode & 0xF000) >> 12;
            var n2 = (opcode & 0x0F00) >> 8;
            var n3 = (opcode & 0x00F0) >> 4;
            var n4 = opcode & 0x000F;
            var address = (ushort) (opcode & 0x0FFF);

            switch (n1, n2, n3, n4)
            {
                case (0x0, 0x0, 0xE, 0x0): // Instruction 0x00E0 - CLS
                    _display.ClearDisplay();
                    break;
                case (0x0, 0x0, 0xE, 0xE): // Instruction 0xEE - RET
                    // TODO - Don't underflow stack
                    _programCounter = _stack[_stackPointer];
                    _stackPointer--;
                    break;
                case (0x1, _, _, _): // Instruction 0x1nnn - JP nnn
                    _programCounter = address;
                    break;
                case (0x2, _, _, _): // Instruction 0x2nnn - CALL nnn
                    _stackPointer++; // TODO - Don't exceed stack size
                    _stack[_stackPointer] = _programCounter;
                    _programCounter = address;
                    break;
                case (0x3, _, _, _): // Instruction 0x3xkk - SE Vx kk
                    if (_registers[n2] == kk)
                    {
                        _programCounter += 2;
                    }

                    break;
                case (0x4, _, _, _): // Instruction 0x4xkk - SNE Vx kk
                    if (_registers[n2] != kk)
                    {
                        _programCounter += 2;
                    }

                    break;
                case (0x5, _, _, _): // Instruction 0x5xy0 - SE Vx Vy
                    if (_registers[n2] == _registers[n3])
                    {
                        _programCounter += 2;
                    }

                    break;
                case (0x6, _, _, _): // Instruction 0x6xkk - LD Vx kk
                    _registers[n2] = kk;
                    break;
                case (0x7, _, _, _): // Instruction 0x7xkk - ADD Vx kk
                    _registers[n2] += kk;
                    break;
                case (0x8, _, _, 0x0): // Instruction 0x8xy0 - LD Vx, Vy
                    _registers[n2] = _registers[n3];
                    break;
                case (0x8, _, _, 0x1): // Instruction 0x8xy0 - OR Vx, Vy
                    _registers[n2] |= _registers[n3];
                    break;
                case (0x8, _, _, 0x2): // Instruction 0x8xy2 - AND Vx, Vy
                    _registers[n2] &= _registers[n3];
                    break;
                case (0x8, _, _, 0x3): // Instruction 0x8xy2 - XOR Vx, Vy
                    _registers[n2] ^= _registers[n3];
                    break;
                case (0x8, _, _, 0x4): // Instruction 0x8xy2 - ADD Vx, Vy
                    var addition = _registers[n2] + _registers[n3];
                    _registers[n2] = (byte) addition;

                    // Set carry flag if result overflowed byte length
                    _registers[0xF] = (byte) (addition > byte.MaxValue ? 1 : 0);
                    break;
                case (0x8, _, _, 0x5): // Instruction 0x8xy5 - SUB Vx, Vy
                    // Set carry flag to NOT borrow
                    _registers[0xF] = (byte) (_registers[n2] >= _registers[n3] ? 1 : 0);

                    _registers[n2] -= _registers[n3];
                    break;
                case (0x8, _, _, 0x6): // Instruction 0x8xy6 - SHR Vx, Vy
                    // Set carry flag to the least significant bit in V[n2]
                    _registers[0xF] = (byte) ((_registers[n2] & 0x1) != 0 ? 1 : 0);
                    _registers[n2] /= 2;
                    break;
                case (0x8, _, _, 0x7): // Instruction 0x8xy5 - SUBN Vx, Vy
                    // Set carry flag to NOT borrow
                    _registers[0xF] = (byte) (_registers[n3] >= _registers[n2] ? 1 : 0);

                    _registers[n3] -= _registers[n2];
                    break;
                case (0x8, _, _, 0xE): // Instruction 0x8xyE - SHL Vx, Vy
                    // Set the carry flag to the most significant bit in V[n2]
                    _registers[0xF] = (byte)((_registers[n2] >> 7) & 0x01);

                    _registers[n2] *= 2;
                    break;
                case (0x9, _, _, 0x0): // Instruction 0x5xy0 - SNE Vx Vy
                    if (_registers[n2] != _registers[n3])
                    {
                        _programCounter += 2;
                    }

                    break;
                case (0xA, _, _, _): // Instruction 0xAnnn - LD I, nnn
                    _i = address;
                    break;
                case (0xB, _, _, _): // Instruction 0xBnnn - JP V0, nnn
                    _programCounter = (ushort) (address + _registers[0x0]);
                    break;
                case (0xC, _, _, _): // Instruction 0xCxkk - RND x, kk
                    _registers[n2] = (byte) (_random.Next(0, 255) & kk);
                    break;
                case (0xD, _, _, _): // Instruction 0xDxyn - DRW Vx,Vy,n
                    var spriteData = _memory.AsSpan().Slice(_i, n4); // TODO - Ensure no array out of bounds
                    _registers[0xF] = (byte) (_display.DrawSprite(_registers[n2], _registers[n3], spriteData) ? 1 : 0);
                    
                    break;
                case (0xE, _, 0x9, 0xE): // Instruction 0xEx9E - SKP Vx
                    if (_keyboard.IsPressed(_registers[n2]))
                    {
                        _programCounter += 2;
                    }

                    break;
                case (0xE, _, 0xA, 0x1): // Instruction 0xExA1 - SKP Vx
                    if (!_keyboard.IsPressed(_registers[n2]))
                    {
                        _programCounter += 2;
                    }

                    break;
                case (0xF, _, 0x0, 0x7): // Instruction 0xFx0A - LD Vx, DT
                    _registers[n2] = _delayTimerRegister;
                    break;
                case (0xF, _, 0x0, 0xA): // Instruction 0xFx0A - LD Vx, K
                    // TODO - Halt execution until key press
                    break;
                case (0xF, _, 0x1, 0x5): // Instruction 0xFx15 - LD DT, Vx
                    _delayTimerRegister = _registers[n2];
                    break;
                case (0xF, _, 0x1, 0x8): // Instruction 0xFx18 - LD ST,Vx
                    _soundTimerRegister = _registers[n2];
                    break;
                case (0xF, _, 0x1, 0xE): // Instruction 0xFx1E - ADD I, Vx
                    _i += _registers[n2];
                    break;
                case (0xF, _, 0x2, 0x9): // Instruction 0xFx29 - LD F, Vx
                    _i = (ushort) (_registers[n2] * 5);
                    break;
                case (0xF, _, 0x3, 0x3): // Instruction 0xFx33 - LD B, Vx
                    _memory[_i] = (byte) (_registers[n2] / 100 % 10);
                    _memory[_i + 1] = (byte) (_registers[n2] / 10 % 10);
                    _memory[_i + 2] = (byte) (_registers[n2] % 10);
                    break;
                case (0xF, _, 0x5, 0x5): // Instruction 0xFx55 - LD[I], Vx
                    Array.Copy(_registers, 0, _memory, _i, n2 + 1);
                    break;
                case (0xF, _, 0x6, 0x5): // Instruction 0xFx55 - LDVx, [I]
                    Array.Copy(_memory, _i, _registers, 0, n2 + 1);
                    break;
            }
        }

        private string StateToString(int opcode)
        {
            
            var registers = string.Join(",", _registers);
            return $"{Convert.ToString(opcode, 16)}, V: {registers}, I {_i}, SP {_stackPointer}";
        }
    }
}