using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI;

namespace CSAllInOneWebRole.Controllers
{
    public class MemeGeneratorController : Controller
    {
        static readonly Dictionary<string, Tuple<string, string>> Memes = new Dictionary<string, Tuple<string, string>>();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost, ActionName("Index")]
        public ActionResult Index_Post(string top, string bottom)
        {
            var identifier = Guid.NewGuid().ToString();
            if (!Memes.ContainsKey(identifier))
            {
                Memes.Add(identifier, new Tuple<string, string>(top, bottom));
            }

            return Content("<a href=\"" + Url.Action("Show", new { id = identifier }) + "\">here's your meme</a>");
        }

        [OutputCache(VaryByParam = "*", Duration = 1, Location = OutputCacheLocation.Downstream)]
        public ActionResult Show(string id)
        {
            Tuple<string, string> data = null;
            if (!Memes.TryGetValue(id, out data))
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            if (Debugger.IsAttached) // Preserve the debug experience
            {
                return Redirect(string.Format("/MemeGenerator/Generate?top={0}&bottom={1}", data.Item1, data.Item2));
            }
            else // Get content from Azure CDN
            {
                return Redirect(string.Format("http://ep-allinone-ak.azureedge.net/MemeGenerator/Generate?top={0}&bottom={1}", data.Item1, data.Item2));
            }
        }

        [OutputCache(VaryByParam = "*", Duration = 3600, Location = OutputCacheLocation.Downstream)]
        public ActionResult Generate(string top, string bottom)
        {
            string imageFilePath = HostingEnvironment.MapPath("~/Content/chuck.bmp");
            Bitmap bitmap = (Bitmap)Image.FromFile(imageFilePath);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                SizeF size = new SizeF();
                using (Font arialFont = FindBestFitFont(bitmap, graphics, top.ToUpperInvariant(), new Font("Arial Narrow", 100), out size))
                {
                    graphics.DrawString(top.ToUpperInvariant(), arialFont, Brushes.White, new PointF(((bitmap.Width - size.Width) / 2), 10f));
                }
                using (Font arialFont = FindBestFitFont(bitmap, graphics, bottom.ToUpperInvariant(), new Font("Arial Narrow", 100), out size))
                {
                    graphics.DrawString(bottom.ToUpperInvariant(), arialFont, Brushes.White, new PointF(((bitmap.Width - size.Width) / 2), bitmap.Height - 10f - arialFont.Height));
                }
            }

            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        private Font FindBestFitFont(Image i, Graphics g, String text, Font font, out SizeF size)
        {
            // Compute actual size, shrink if needed
            while (true)
            {
                size = g.MeasureString(text, font);

                // It fits, back out
                if (size.Height < i.Height &&
                     size.Width < i.Width) { return font; }

                // Try a smaller font (90% of old size)
                Font oldFont = font;
               font = new Font(font.Name, (float)(font.Size * .9), font.Style);
                oldFont.Dispose();
            }
        }
    }
}