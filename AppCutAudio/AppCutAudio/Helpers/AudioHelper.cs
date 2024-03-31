using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AppCutAudio.Helpers
{
    public static class AudioHelper
    {
        public static void PlayMp3FromUrl(string url)
        {
            using (Stream ms = new MemoryStream())
            {
                using (Stream stream = WebRequest.Create(url).GetResponse().GetResponseStream())
                {
                    byte[] buffer = new byte[32768];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                }

                ms.Position = 0;
                using (WaveStream blockAlignedStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(ms))))
                {
                    using (WaveOut waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                    {
                        waveOut.Init(blockAlignedStream);
                        waveOut.Play();
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                }
            }
        }
        public static void CrearDemo(string rutaArchivo, string carpetaProyecto, TimeSpan duration)
        {
            if (!Directory.Exists(carpetaProyecto))
                Directory.CreateDirectory(carpetaProyecto);

            string rutaArchivoSalida = Path.Combine(carpetaProyecto, "demo.mp3");
            var nivelSilencio = 0.03f; // Ajusta este valor según tus necesidades

            using (var reader = new AudioFileReader(rutaArchivo))
            using (var writer = new LameMP3FileWriter(rutaArchivoSalida, reader.WaveFormat, LAMEPreset.ABR_128))
            {
                var buffer = new float[reader.WaveFormat.SampleRate * 4];
                int muestrasLeidas;
                TimeSpan duracionGuardada = TimeSpan.Zero;

                while (duracionGuardada < duration && (muestrasLeidas = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (!EsSilencio(buffer, muestrasLeidas, nivelSilencio))
                    {
                        // Convertir el arreglo de float[] a byte[]
                        var bufferBytes = new byte[muestrasLeidas * sizeof(float)];
                        Buffer.BlockCopy(buffer, 0, bufferBytes, 0, muestrasLeidas * sizeof(float));

                        writer.Write(bufferBytes, 0, bufferBytes.Length);
                        duracionGuardada += TimeSpan.FromSeconds((double)muestrasLeidas / reader.WaveFormat.SampleRate);
                    }
                }
            }
        }
        public static void DividirAudio(string rutaArchivo, string carpetaProyecto, TimeSpan duracionMaxima)
        {
            if(!Directory.Exists(carpetaProyecto))
                Directory.CreateDirectory(carpetaProyecto);
            
            using (var reader = new AudioFileReader(rutaArchivo))
            {
                var duracionArchivo = reader.TotalTime;
                var inicio = TimeSpan.Zero;

                int parte = 0;
                while (inicio < duracionArchivo)
                {
                    var fin = inicio + duracionMaxima < duracionArchivo ? inicio + duracionMaxima : duracionArchivo;
                    var duracion = fin - inicio;

                    string rutaArchivoSalida = Path.Combine(carpetaProyecto,$"Parte_{parte}.mp3");
                    GuardarPartesAudio(reader, inicio, duracion, rutaArchivoSalida);

                    inicio = fin;
                    parte++;
                }
            }
        }
        private static void GuardarPartesAudio(AudioFileReader reader, TimeSpan inicio, TimeSpan duracion, string rutaArchivoSalida)
        {
            reader.CurrentTime = inicio;
            var duracionEnMuestras = (int)(duracion.TotalSeconds * reader.WaveFormat.SampleRate);
            var buffer = new float[duracionEnMuestras];
            int muestrasLeidas = 0;

            while (muestrasLeidas < duracionEnMuestras)
            {
                muestrasLeidas += reader.Read(buffer, muestrasLeidas, duracionEnMuestras - muestrasLeidas);
            }

            // Convertir el arreglo de float[] a byte[]
            var bufferBytes = new byte[muestrasLeidas * sizeof(float)];
            Buffer.BlockCopy(buffer, 0, bufferBytes, 0, muestrasLeidas * sizeof(float));

            // Crear un nuevo formato de onda con la nueva frecuencia de muestreo
            var nuevoFormato = new WaveFormat(22050, reader.WaveFormat.Channels);

            // Usar MediaFoundationResampler para cambiar la frecuencia de muestreo
            using (var resampler = new MediaFoundationResampler(reader, nuevoFormato))
            {
                // Asegurarse de que el resampler no intente enviar más muestras de las que están disponibles
                resampler.ResamplerQuality = 60;

                using (var writer = new LameMP3FileWriter(rutaArchivoSalida, resampler.WaveFormat, LAMEPreset.ABR_128))
                {
                    // Escribir las muestras en el archivo MP3
                    int bytesRead;
                    var bufferResampler = new byte[resampler.WaveFormat.AverageBytesPerSecond * 4];
                    while ((bytesRead = resampler.Read(bufferResampler, 0, bufferResampler.Length)) > 0)
                    {
                        writer.Write(bufferResampler, 0, bytesRead);
                    }
                }
            }
        }
        private static void GuardarDemo(AudioFileReader reader, TimeSpan inicio, TimeSpan duracion, string rutaArchivoSalida)
        {
            reader.CurrentTime = inicio;
            var duracionEnMuestras = (int)(duracion.TotalSeconds * reader.WaveFormat.SampleRate);
            var buffer = new float[duracionEnMuestras];
            int muestrasLeidas = 0;

            while (muestrasLeidas < duracionEnMuestras)
            {
                muestrasLeidas += reader.Read(buffer, muestrasLeidas, duracionEnMuestras - muestrasLeidas);
            }

            // Convertir el arreglo de float[] a byte[]
            var bufferBytes = new byte[muestrasLeidas * sizeof(float)];
            Buffer.BlockCopy(buffer, 0, bufferBytes, 0, muestrasLeidas * sizeof(float));

            using (var writer = new LameMP3FileWriter(rutaArchivoSalida, reader.WaveFormat, LAMEPreset.ABR_128))
            {
                writer.Write(bufferBytes, 0, bufferBytes.Length);
            }
        }
        private static bool EsSilencio(float[] buffer, int muestrasLeidas, float nivelSilencio)
        {
            for (int i = 0; i < muestrasLeidas; i++)
            {
                if (Math.Abs(buffer[i]) > nivelSilencio)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
