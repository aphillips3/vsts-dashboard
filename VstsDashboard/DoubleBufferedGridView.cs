using System.Windows.Forms;

namespace VstsDashboard
{
    public class DoubleBufferedGridView : DataGridView
    {
        public DoubleBufferedGridView()
        {
            DoubleBuffered = true;
        }
    }
}
