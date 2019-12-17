namespace Chip8.VM.Keyboards
{
    public class NullKeyboard : IKeyboard
    {
        public bool IsPressed(byte keyValue)
        {
            return false;
        }
    }
}