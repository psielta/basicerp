using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Serilog;

namespace WebApplicationBasic.Services
{
    public interface IImageProcessingService
    {
        Stream ResizeImage(Stream imageStream, int maxWidth, int maxHeight, long quality = 85L);
    }

    public class ImageProcessingService : IImageProcessingService
    {
        /// <summary>
        /// Redimensiona uma imagem mantendo a proporção e otimizando a qualidade
        /// </summary>
        /// <param name="imageStream">Stream da imagem original</param>
        /// <param name="maxWidth">Largura máxima em pixels</param>
        /// <param name="maxHeight">Altura máxima em pixels</param>
        /// <param name="quality">Qualidade da compressão JPEG (0-100)</param>
        /// <returns>Stream da imagem redimensionada</returns>
        public Stream ResizeImage(Stream imageStream, int maxWidth, int maxHeight, long quality = 85L)
        {
            try
            {
                // Carregar a imagem original
                using (var originalImage = Image.FromStream(imageStream))
                {
                    // Calcular novo tamanho mantendo proporção
                    var ratioX = (double)maxWidth / originalImage.Width;
                    var ratioY = (double)maxHeight / originalImage.Height;
                    var ratio = Math.Min(ratioX, ratioY);

                    // Se a imagem já é menor que o máximo, retornar original
                    if (ratio >= 1.0)
                    {
                        imageStream.Position = 0;
                        return imageStream;
                    }

                    var newWidth = (int)(originalImage.Width * ratio);
                    var newHeight = (int)(originalImage.Height * ratio);

                    // Criar nova imagem redimensionada
                    using (var newImage = new Bitmap(newWidth, newHeight))
                    {
                        using (var graphics = Graphics.FromImage(newImage))
                        {
                            // Configurar alta qualidade de renderização
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            // Desenhar imagem redimensionada
                            using (var wrapMode = new ImageAttributes())
                            {
                                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                                graphics.DrawImage(originalImage,
                                    new Rectangle(0, 0, newWidth, newHeight),
                                    0, 0, originalImage.Width, originalImage.Height,
                                    GraphicsUnit.Pixel, wrapMode);
                            }
                        }

                        // Salvar em novo stream com compressão otimizada
                        var outputStream = new MemoryStream();

                        // Detectar formato e aplicar compressão apropriada
                        if (IsPng(originalImage))
                        {
                            // PNG - manter formato sem perda
                            newImage.Save(outputStream, ImageFormat.Png);
                        }
                        else
                        {
                            // JPEG - aplicar compressão com qualidade especificada
                            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                            var encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                            newImage.Save(outputStream, jpegEncoder, encoderParameters);
                        }

                        outputStream.Position = 0;
                        return outputStream;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "IMAGE_PROCESSING_ERROR: Erro ao processar imagem com maxWidth={MaxWidth}, maxHeight={MaxHeight}",
                    maxWidth, maxHeight);
                throw new Exception($"Erro ao processar imagem: {ex.Message}", ex);
            }
        }

        private bool IsPng(Image image)
        {
            return image.RawFormat.Equals(ImageFormat.Png);
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}