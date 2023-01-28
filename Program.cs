using RWAPI;
using System;
using System.Diagnostics;
using System.Windows.Forms;

#if !DEBUG

try
{
#endif

using var game = new RainMap.Main();
game.Run();

#if !DEBUG

}
catch (Exception ex)
{

    MessageBox.Show(
$@"Caught fatal exception: 

{ex.GetType().Name}: {ex.Message}
{ex.StackTrace}
", "RainMap has crashed");
}

#endif