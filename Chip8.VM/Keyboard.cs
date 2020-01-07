using System;
using System.Diagnostics;

namespace Chip8.VM
{
    public class Keyboard
    {
        private bool[] _keys = new bool[16];
        
        internal void ClearKeyboard()
        {
            Array.Clear(_keys, 0, _keys.Length);
        }

        internal byte? FirstKeyPressed()
        {
            for (var ii = 0; ii < _keys.Length; ii++)
            {
                if (_keys[ii]) return (byte)ii;
            }

            return null;
        }

        internal bool IsKeyPressed(byte key)
        {
            return _keys[key];
        }

        internal void KeyDown(byte key)
        {
            Trace.WriteLine("Key pressed: " + Convert.ToString(key, 16));
            _keys[key] = true;
        }

        internal void KeyUp(byte key)
        {
            Trace.WriteLine("Key released: " + Convert.ToString(key, 16));
            _keys[key] = false;
        }

        public override string ToString()
        {
            return "Keyboard: " + string.Join(",", _keys);
        }
    }
}