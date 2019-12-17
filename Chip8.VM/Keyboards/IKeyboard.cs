namespace Chip8.VM.Keyboards
{
    public interface IKeyboard
    {
        public bool IsPressed(byte keyValue);
    }
}