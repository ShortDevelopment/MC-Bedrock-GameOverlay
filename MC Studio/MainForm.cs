using System.Windows.Forms;

namespace MC_Studio
{
    public partial class MainForm : Form
    {

        public UIWrapper UIWrapper { get; private set; }

        public MainForm()
        {
            InitializeComponent();

            UIWrapper = new UIWrapper(webView21);
            UIWrapper.LoadContent("index.html");
        }
    }
}
