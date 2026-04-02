using WBitmap = WUFF.Images.Bitmaps.Bitmap;
using WIcon = WUFF.Images.Icons.Icon;
using WUFF.Err;

namespace WUFF_Display
{
    public partial class Form1 : Form
    {
        private readonly string[] _bmp_list = [.. Directory.GetFiles("C:\\Users\\msvan\\Desktop\\WUFF\\BMP\\bmpsuite-2.8\\bmpsuite-2.8\\q\\")];
        //private readonly string[] _bmp_list = ["C:\\Users\\msvan\\Desktop\\WUFF\\BMP\\GOOD\\rgb32bfdef.bmp"];
        private int index = 0;

        private const int MAX_WIDTH = 640;
        private const int MAX_HEIGHT = 380;

        public Form1()
        {
            InitializeComponent();
        }

        private void BMPTesting()
        {
            string name = _bmp_list[index++];
            label1.Text = name;
            Result<WBitmap> result = WBitmap.TryLoad(name);
            if (index >= _bmp_list.Length) index = 0;

            if (result.Passed)
            {
                WBitmap bitmap = result.GetResult();
                Bitmap transfer = new(bitmap.Width, bitmap.Height);

                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        transfer.SetPixel(x, y, bitmap[x, y].ToColor());
                    }
                }

                int currWidth = bitmap.Width * 2;
                int currHeight = bitmap.Height * 2;

                if (currWidth > MAX_WIDTH)
                {
                    currWidth = MAX_WIDTH;
                    currHeight = (int)((double)MAX_WIDTH / currWidth * currHeight);
                }

                pictureBox1.Width = currWidth;
                pictureBox1.Height = currHeight;
                pictureBox1.Image = transfer;
            }
            else
            {
                label1.Text += result.GetReason();
                pictureBox1.Image = null;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            BMPTesting();
        }

        private void ICOTesting()
        {
            WIcon icons = WIcon.Load("C:\\Users\\msvan\\Desktop\\WUFF\\ICO\\DTU.ICO");
            WBitmap icon = icons[index++]; 
            Bitmap transfer = new(icon.Width, icon.Height);

            if (index >= icons.Count) index = 0;

            for (int x = 0; x < icon.Width; x++)
            {
                for (int y = 0; y < icon.Height; y++)
                {
                    transfer.SetPixel(x, y, icon[x, y].ToColor());
                }
            }

            pictureBox1.Width = transfer.Width * 2;
            pictureBox1.Height = transfer.Height * 2;
            pictureBox1.Image = transfer;
        }
    }
}
