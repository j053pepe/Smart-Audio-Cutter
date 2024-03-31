using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace AppCutAudio.Helpers
{
    public static class ImagenHelper
    {
        public static void UnirImagenes(List<BitmapImage> bitmapImages, string rutaSalida)
        {
            // Convertir cada BitmapImage a Image<Rgba32>
            var imagenes = bitmapImages.Select(bitmapImage =>
            {
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                    encoder.Save(stream);
                    stream.Position = 0;
                    return Image.Load<Rgba32>(stream);
                }
            }).ToList();

            // Calcular el ancho total y la altura máxima
            int anchoTotal = imagenes.Sum(imagen => imagen.Width);
            int alturaMaxima = imagenes.Max(imagen => imagen.Height);

            // Crear una nueva imagen con el ancho total y la altura máxima
            using (var imagenUnida = new Image<Rgba32>(anchoTotal, alturaMaxima))
            {
                int x = 0;
                foreach (var imagen in imagenes)
                {
                    // Dibujar cada imagen en la posición correspondiente
                    imagenUnida.Mutate(ctx => ctx.DrawImage(imagen, new Point(x, 0), 1));
                    x += imagen.Width;
                }

                // Guardar la imagen unida
                imagenUnida.Save(rutaSalida);
            }

            // Liberar los recursos de las imágenes cargadas
            foreach (var imagen in imagenes)
            {
                imagen.Dispose();
            }
        }
    }
}
