using System;
using System.Windows.Forms;

namespace NexusForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new FormIntro());
        }
    }
}