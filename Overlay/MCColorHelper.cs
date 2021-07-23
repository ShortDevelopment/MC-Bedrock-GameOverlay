using System.Diagnostics;
using System.Windows.Media;

namespace Overlay
{
    public class MCColorHelper
    {
        public static Color GetColorFromBlock(string id)
        {
            //Debug.Print(id);
            switch (id)
            {
                case "stone":
                    return Colors.Black;
                case "sand":
                    return Colors.Yellow;
                case "grass":
                    return Colors.Green;
                case "dirt":
                    return Colors.Brown;
                case "deepslate":
                    return Colors.Black;
                default:
                    return Colors.Magenta;
            }
        }
    }
}
