using System;

namespace Chip8.VM
{
    /// <summary>
    /// Program types define where the program starts in memory
    /// </summary>
    public enum ProgramType
    {
        Chip8,
        ETI660Chip8
    }
    
    public static class ProgramTypeExtensions
    {
        public static ushort MemoryStartAddress(this ProgramType programType)
        {
            return programType switch
            {
                ProgramType.Chip8 => 0x200,
                ProgramType.ETI660Chip8 => 0x600,
                _ => throw new ArgumentOutOfRangeException(nameof(programType), programType, null)
            };
        }
    }
}