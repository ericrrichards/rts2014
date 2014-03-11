namespace RTS {
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class Util {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        public static bool IsKeyDown(Keys key) {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }
    }
}