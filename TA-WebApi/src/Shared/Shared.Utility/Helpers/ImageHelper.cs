using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Utility.Helpers
{
    public class ImageHelper
    {
        public static string ToBase64(Image image)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, image.RawFormat); // Save in original format
                    byte[] imageBytes = ms.ToArray();
                    return Convert.ToBase64String(imageBytes); // Convert to Base64
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
            
        public static byte[] ToByteArray(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat); // Save in original format
                return ms.ToArray();
            }
        }

        public static string ToBase64FromUrl(string url)
        {
            try
            {
                var image = File.ReadAllBytes(url);
                return Convert.ToBase64String(image); // Convert to Base64

            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
    }
}
