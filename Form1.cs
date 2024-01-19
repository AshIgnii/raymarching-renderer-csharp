using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace renderer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        float SdSphere(Vector3 p, float s)
        {
            return p.Length() - s;
        }

        float Map(Vector3 p)
        {
            return SdSphere(p, 1f);
        }

        private byte ClampToByte(float value)
        {
            return (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
        }

        bool done = false;
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (done) return;
            Vector2 screen = new Vector2(this.Width, this.Height);

            int numThreads = Environment.ProcessorCount;
            int linesPerThread = (int)screen.Y / numThreads;

            var tasks = new List<Task>();

            for (int t = 0; t < numThreads; t++)
            {
                int startY = t * linesPerThread;
                int endY = (t + 1) * linesPerThread;

                tasks.Add(Task.Factory.StartNew(() =>
                {
                    RenderLines(e.Graphics, screen, startY, endY);
                }));
            }

            Task.WaitAll(tasks.ToArray());
            done = true;
        }

        private void RenderLines(Graphics graphics, Vector2 screen, int startY, int endY)
        {
            for (int y = startY; y < endY; y++)
            {
                Vector2 fragCoord = new Vector2(0, y);

                for (fragCoord.X = 0; fragCoord.X < screen.X; fragCoord.X++)
                {
                    Vector2 uv = new Vector2(
                        (fragCoord.X + 0.5f) / screen.X * 2.0f - 1.0f,
                        1.0f - (fragCoord.Y + 0.5f) / screen.Y * 2.0f
                    );

                    Vector3 camPos = new Vector3(0, 0, -3);
                    float fov = 1f;
                    Vector3 rayDir = Vector3.Normalize(new Vector3(uv * fov, 1));

                    float travelDis = 0f;

                    for (int i = 0; i < 100; i++)
                    {
                        Vector3 p = camPos + rayDir * travelDis;

                        float distanceFromObj = Map(p);

                        travelDis += distanceFromObj;

                        if (Math.Abs(distanceFromObj) < .001f || travelDis > 100f)
                            break;
                    }

                    float col = Math.Clamp(travelDis * 10f, 0f, 255f) / 255f;

                    Color currentColor = Color.FromArgb(255, (int)(col * 255), (int)(col * 255), (int)(col * 255));

                    using (Pen cPen = new Pen(currentColor))
                    {
                        cPen.Width = 1;

                        using (Bitmap bmp = new Bitmap(1, 1))
                        {
                            using (Graphics bmpGraphics = Graphics.FromImage(bmp))
                            {
                                bmpGraphics.DrawEllipse(cPen, 0, 0, 1, 1);
                            }
                            lock (graphics)
                            {
                                graphics.DrawImage(bmp, fragCoord.X, fragCoord.Y);
                            }
                        }
                    }
                }
            }
        }
    }
}