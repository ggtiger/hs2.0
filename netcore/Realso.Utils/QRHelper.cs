using System;
using System.IO;
using QRCoder;

namespace Realso.Utils
{
  public class QRHelper
  {
    /// <summary>
    /// 生成二维码并保存为PNG文件（跨平台，不依赖System.Drawing/GDI+）
    /// </summary>
    public static void SaveQR(string filePath, int pixelsPerModule, string content)
    {
      QRCodeGenerator qrGenerator = new QRCodeGenerator();
      QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
      PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
      byte[] pngBytes = qrCode.GetGraphic(pixelsPerModule);
      File.WriteAllBytes(filePath, pngBytes);
    }

#if ENABLE_SYSTEM_DRAWING
    // 以下方法依赖 System.Drawing（GDI+），仅在 Windows 或已安装 libgdiplus 的环境可用
    // 编译时需定义 ENABLE_SYSTEM_DRAWING 常量

    public static System.Drawing.Bitmap CreateQR(int pixelsPerModule, string info, System.Drawing.Color qrColor, System.Drawing.Color qrBackgroundColor, System.Drawing.Bitmap logo, int iconSizePercent = 15, int iconBorderWidth = 6)
    {
      QRCodeGenerator qrGenerator = new QRCodeGenerator();
      QRCodeData qrCodeData = qrGenerator.CreateQrCode(info, QRCodeGenerator.ECCLevel.Q);
      QRCode qrCode = new QRCode(qrCodeData);
      System.Drawing.Bitmap qrCodeImage = qrCode.GetGraphic(pixelsPerModule, qrColor, qrBackgroundColor, logo, iconSizePercent, iconBorderWidth, true);
      return qrCodeImage;
    }

    public static System.Drawing.Bitmap CreateQR(int pixelsPerModule, string info, System.Drawing.Color qrColor, System.Drawing.Color qrBackgroundColor)
    {
      QRCodeGenerator qrGenerator = new QRCodeGenerator();
      QRCodeData qrCodeData = qrGenerator.CreateQrCode(info, QRCodeGenerator.ECCLevel.Q);
      QRCode qrCode = new QRCode(qrCodeData);
      System.Drawing.Bitmap qrCodeImage = qrCode.GetGraphic(pixelsPerModule, qrColor, qrBackgroundColor, true);
      return qrCodeImage;
    }

    public static System.Drawing.Bitmap CreateQR(int pixelsPerModule, string info)
    {
      return CreateQR(pixelsPerModule, info, System.Drawing.Color.Black, System.Drawing.Color.White);
    }
#endif
  }
}
