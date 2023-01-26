using System;
using System.Diagnostics;
using System.Windows.Forms;

try
{
    using var game = new RainMap.Main();
    game.Run();
}
catch (Exception ex)
{
//    if (Debugger.IsAttached)
//        throw;

    MessageBox.Show(
$@"Caught fatal exception: 

{ex.GetType().Name}: {ex.Message}
{ex.StackTrace}
", "RainMap has crashed");
}
